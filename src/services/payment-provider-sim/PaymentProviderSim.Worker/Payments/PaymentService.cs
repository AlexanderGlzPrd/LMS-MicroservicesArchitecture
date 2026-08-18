using Microsoft.EntityFrameworkCore;
using PaidEnrollment.Contracts.V1;
using PaymentProviderSim.Contracts.V1;
using PaymentProviderSim.Worker.Messaging;
using PaymentProviderSim.Worker.Persistence;
using PaymentProviderSim.Worker.Rules;
namespace PaymentProviderSim.Worker.Payments;

internal sealed class PaymentService(
    PaymentsDbContext context,
    InboxRecorder inbox,
    OutboxWriter outbox,
    UnitOfWork unitOfWork,
    SimulatorRules rules,
    TimeProvider timeProvider,
    ILogger<PaymentService> logger)
{
    internal const string DeclinedReason = "DeclinedByProvider";

    internal const string CaptureRejectedReason = "CaptureRejectedByProvider";

    internal const string NotAuthorizedReason = "NotAuthorized";

    internal const string RefundRejectedReason = "RefundRejectedByProvider";

    internal const string NotCapturedReason = "NotCaptured";

    internal const string NotFoundStatus = "NotFound";

    private static readonly EventId CollisionEvent = new(6001, "payment-id-collision");

    private static readonly EventId SuppressedReplyEvent = new(6002, "suppressed-reply");

    private static readonly EventId IgnoredCommandEvent = new(6003, "ignored-payment-command");

    public async Task AuthorizeAsync(
        Guid messageId,
        string messageType,
        AuthorizePayment command,
        CancellationToken cancellationToken)
    {
        var payment = await FindAsync(command.PaymentId, cancellationToken);

        if (payment is not null)
        {
            EnsureConsistent(
                payment, command.PurchaseId, command.PaymentId, command.Amount, command.Currency);
        }

        if (await HasBeenProcessedAsync(messageId, messageType, cancellationToken))
        {
            return;
        }

        var now = timeProvider.GetUtcNow();

        if (payment is null)
        {
            ApplyAuthorization(command, now);
        }
        else
        {
            ResendAuthorizationOutcome(payment, now);
        }

        inbox.Record(messageId, messageType, now);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CaptureAsync(
        Guid messageId,
        string messageType,
        CapturePayment command,
        CancellationToken cancellationToken)
    {
        var payment = await FindAsync(command.PaymentId, cancellationToken);

        if (payment is not null)
        {
            EnsureConsistent(payment, command.PurchaseId, command.PaymentId);
        }

        if (await HasBeenProcessedAsync(messageId, messageType, cancellationToken))
        {
            return;
        }

        var now = timeProvider.GetUtcNow();

        ApplyCapture(command, payment, now);

        inbox.Record(messageId, messageType, now);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task VoidAsync(
        Guid messageId,
        string messageType,
        VoidAuthorization command,
        CancellationToken cancellationToken)
    {
        var payment = await FindAsync(command.PaymentId, cancellationToken);

        if (payment is not null)
        {
            EnsureConsistent(payment, command.PurchaseId, command.PaymentId);
        }

        if (await HasBeenProcessedAsync(messageId, messageType, cancellationToken))
        {
            return;
        }

        var now = timeProvider.GetUtcNow();

        ApplyVoid(command, payment, now);

        inbox.Record(messageId, messageType, now);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RefundAsync(
        Guid messageId,
        string messageType,
        RefundPayment command,
        CancellationToken cancellationToken)
    {
        var payment = await FindAsync(command.PaymentId, cancellationToken);

        if (payment is not null)
        {
            EnsureConsistent(payment, command.PurchaseId, command.PaymentId);
        }

        if (await HasBeenProcessedAsync(messageId, messageType, cancellationToken))
        {
            return;
        }

        var now = timeProvider.GetUtcNow();

        ApplyRefund(command, payment, now);

        inbox.Record(messageId, messageType, now);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ReportStatusAsync(
        Guid messageId,
        string messageType,
        GetPaymentStatus command,
        CancellationToken cancellationToken)
    {
        var payment = await FindAsync(command.PaymentId, cancellationToken);

        if (payment is not null)
        {
            EnsureConsistent(payment, command.PurchaseId, command.PaymentId);
        }

        if (await HasBeenProcessedAsync(messageId, messageType, cancellationToken))
        {
            return;
        }

        var now = timeProvider.GetUtcNow();

        outbox.Enqueue(payment is null
            ? new PaymentStatusReported
            {
                PurchaseId = command.PurchaseId,
                PaymentId = command.PaymentId,
                Status = NotFoundStatus,
                AuthorizedAt = null,
                CapturedAt = null,
                VoidedAt = null,
                RefundedAt = null,
                FailureReason = null,
                OccurredAt = now,
            }
            : new PaymentStatusReported
            {
                PurchaseId = payment.PurchaseId,
                PaymentId = payment.PaymentId,
                Status = payment.Status.ToString(),
                AuthorizedAt = payment.AuthorizedAt,
                CapturedAt = payment.CapturedAt,
                VoidedAt = payment.VoidedAt,
                RefundedAt = payment.RefundedAt,
                FailureReason = payment.LastFailureReason,
                OccurredAt = now,
            });

        inbox.Record(messageId, messageType, now);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuthorization(AuthorizePayment command, DateTimeOffset now)
    {
        var rule = rules.For(command.Amount);

        if (rule == SimulatorRule.DeclineAuthorization)
        {
            var declined = Payment.Decline(
                command.PaymentId,
                command.PurchaseId,
                command.Amount,
                command.Currency,
                DeclinedReason,
                now);

            context.Payments.Add(declined);

            outbox.Enqueue(new PaymentDeclined
            {
                PurchaseId = declined.PurchaseId,
                PaymentId = declined.PaymentId,
                Reason = DeclinedReason,
                OccurredAt = now,
            });

            return;
        }

        var payment = Payment.Authorize(
            command.PaymentId, command.PurchaseId, command.Amount, command.Currency, now);

        context.Payments.Add(payment);

        if (rule == SimulatorRule.SilentAuthorization
            && TrySuppressReply(payment, now, "autorizacion"))
        {
            return;
        }

        outbox.Enqueue(new PaymentAuthorized
        {
            PurchaseId = payment.PurchaseId,
            PaymentId = payment.PaymentId,
            AuthorizedAt = payment.AuthorizedAt!.Value,
            OccurredAt = now,
        });
    }

    private void ResendAuthorizationOutcome(Payment payment, DateTimeOffset now)
    {
        if (payment.Status == PaymentStatus.Declined)
        {
            outbox.Enqueue(new PaymentDeclined
            {
                PurchaseId = payment.PurchaseId,
                PaymentId = payment.PaymentId,
                Reason = payment.LastFailureReason ?? DeclinedReason,
                OccurredAt = now,
            });

            return;
        }

        outbox.Enqueue(new PaymentAuthorized
        {
            PurchaseId = payment.PurchaseId,
            PaymentId = payment.PaymentId,
            AuthorizedAt = payment.AuthorizedAt!.Value,
            OccurredAt = now,
        });
    }

    private void ApplyCapture(CapturePayment command, Payment? payment, DateTimeOffset now)
    {
        if (payment is null)
        {
            EnqueueCaptureFailed(command.PurchaseId, command.PaymentId, NotAuthorizedReason, now);

            return;
        }

        switch (payment.Status)
        {
            case PaymentStatus.Authorized:
                CaptureAuthorized(payment, now);

                break;

            case PaymentStatus.Captured:
                outbox.Enqueue(new PaymentCaptured
                {
                    PurchaseId = payment.PurchaseId,
                    PaymentId = payment.PaymentId,
                    CapturedAt = payment.CapturedAt!.Value,
                    OccurredAt = now,
                });

                break;

            case PaymentStatus.CaptureFailed:
                EnqueueCaptureFailed(
                    payment.PurchaseId,
                    payment.PaymentId,
                    payment.LastFailureReason ?? CaptureRejectedReason,
                    now);

                break;

            default:
                EnqueueCaptureFailed(
                    payment.PurchaseId, payment.PaymentId, NotAuthorizedReason, now);

                break;
        }
    }

    private void CaptureAuthorized(Payment payment, DateTimeOffset now)
    {
        var rule = rules.For(payment.Amount);

        if (rule == SimulatorRule.FailCapture)
        {
            payment.FailCapture(CaptureRejectedReason, now);

            EnqueueCaptureFailed(
                payment.PurchaseId, payment.PaymentId, CaptureRejectedReason, now);

            return;
        }

        payment.Capture(now);

        if (rule == SimulatorRule.SilentCapture && TrySuppressReply(payment, now, "captura"))
        {
            return;
        }

        outbox.Enqueue(new PaymentCaptured
        {
            PurchaseId = payment.PurchaseId,
            PaymentId = payment.PaymentId,
            CapturedAt = payment.CapturedAt!.Value,
            OccurredAt = now,
        });
    }

    private void ApplyVoid(VoidAuthorization command, Payment? payment, DateTimeOffset now)
    {
        if (payment is null)
        {
            LogIgnored(command.PurchaseId, command.PaymentId, nameof(VoidAuthorization), null);

            return;
        }

        switch (payment.Status)
        {
            case PaymentStatus.Authorized:
            case PaymentStatus.CaptureFailed:
                payment.Void(now);

                outbox.Enqueue(new AuthorizationVoided
                {
                    PurchaseId = payment.PurchaseId,
                    PaymentId = payment.PaymentId,
                    VoidedAt = payment.VoidedAt!.Value,
                    OccurredAt = now,
                });

                break;

            case PaymentStatus.Voided:
                outbox.Enqueue(new AuthorizationVoided
                {
                    PurchaseId = payment.PurchaseId,
                    PaymentId = payment.PaymentId,
                    VoidedAt = payment.VoidedAt!.Value,
                    OccurredAt = now,
                });

                break;

            default:
                LogIgnored(
                    payment.PurchaseId,
                    payment.PaymentId,
                    nameof(VoidAuthorization),
                    payment.Status);

                break;
        }
    }

    private void ApplyRefund(RefundPayment command, Payment? payment, DateTimeOffset now)
    {
        if (payment is null)
        {
            EnqueueRefundFailed(command.PurchaseId, command.PaymentId, NotCapturedReason, now);

            return;
        }

        switch (payment.Status)
        {
            case PaymentStatus.Captured:
                RefundCaptured(payment, now);

                break;

            case PaymentStatus.Refunded:
                outbox.Enqueue(new PaymentRefunded
                {
                    PurchaseId = payment.PurchaseId,
                    PaymentId = payment.PaymentId,
                    RefundedAt = payment.RefundedAt!.Value,
                    OccurredAt = now,
                });

                break;

            default:
                EnqueueRefundFailed(
                    payment.PurchaseId, payment.PaymentId, NotCapturedReason, now);

                break;
        }
    }

    private void RefundCaptured(Payment payment, DateTimeOffset now)
    {
        var rule = rules.For(payment.Amount);

        if (rule == SimulatorRule.FailRefund)
        {
            payment.FailRefund(RefundRejectedReason, now);

            EnqueueRefundFailed(
                payment.PurchaseId, payment.PaymentId, RefundRejectedReason, now);

            return;
        }

        payment.Refund(now);

        if (rule == SimulatorRule.SilentRefund && TrySuppressReply(payment, now, "reembolso"))
        {
            return;
        }

        outbox.Enqueue(new PaymentRefunded
        {
            PurchaseId = payment.PurchaseId,
            PaymentId = payment.PaymentId,
            RefundedAt = payment.RefundedAt!.Value,
            OccurredAt = now,
        });
    }

    private void EnqueueCaptureFailed(
        Guid purchaseId, Guid paymentId, string reason, DateTimeOffset now) =>
        outbox.Enqueue(new CaptureFailed
        {
            PurchaseId = purchaseId,
            PaymentId = paymentId,
            Reason = reason,
            OccurredAt = now,
        });

    private void EnqueueRefundFailed(
        Guid purchaseId, Guid paymentId, string reason, DateTimeOffset now) =>
        outbox.Enqueue(new RefundFailed
        {
            PurchaseId = purchaseId,
            PaymentId = paymentId,
            Reason = reason,
            OccurredAt = now,
        });

    private bool TrySuppressReply(Payment payment, DateTimeOffset now, string operation)
    {
        if (payment.SuppressedReplyCount >= rules.SilentReplyCount)
        {
            return false;
        }

        payment.RecordSuppressedReply(now);

        logger.LogWarning(
            SuppressedReplyEvent,
            "La {Operation} del pago {PaymentId} de la compra {PurchaseId} se aplico y se " +
            "persistio, y su respuesta se suprimio deliberadamente.",
            operation,
            payment.PaymentId,
            payment.PurchaseId);

        return true;
    }

    private Task<Payment?> FindAsync(Guid paymentId, CancellationToken cancellationToken) =>
        context.Payments.FirstOrDefaultAsync(
            payment => payment.PaymentId == paymentId, cancellationToken);

    private async Task<bool> HasBeenProcessedAsync(
        Guid messageId, string messageType, CancellationToken cancellationToken)
    {
        if (!await inbox.HasBeenProcessedAsync(messageId, cancellationToken))
        {
            return false;
        }

        logger.LogInformation(
            "El mensaje {MessageId} de tipo {MessageType} ya estaba procesado. No se repite.",
            messageId,
            messageType);

        return true;
    }

    private void EnsureConsistent(
        Payment payment,
        Guid purchaseId,
        Guid paymentId,
        decimal? amount = null,
        string? currency = null)
    {
        string? discrepancy = null;

        if (payment.PurchaseId != purchaseId)
        {
            discrepancy = "PurchaseId";
        }
        else if (amount is not null && payment.Amount != amount.Value)
        {
            discrepancy = "Amount";
        }
        else if (currency is not null
            && !string.Equals(payment.Currency, currency, StringComparison.Ordinal))
        {
            discrepancy = "Currency";
        }

        if (discrepancy is null)
        {
            return;
        }

        logger.LogError(
            CollisionEvent,
            "El comando reutiliza el PaymentId {PaymentId} con un {Discrepancy} distinto del " +
            "almacenado. Compra entrante {IncomingPurchaseId}, compra almacenada " +
            "{StoredPurchaseId}. No se aplica ningun efecto.",
            paymentId,
            discrepancy,
            purchaseId,
            payment.PurchaseId);

        throw new PaymentIdCollisionException(paymentId, discrepancy);
    }

    private void LogIgnored(
        Guid purchaseId, Guid paymentId, string command, PaymentStatus? status) =>
        logger.LogWarning(
            IgnoredCommandEvent,
            "El comando {Command} del pago {PaymentId} de la compra {PurchaseId} no aplica " +
            "sobre el estado {Status}. No se responde nada; la Saga lo reconcilia.",
            command,
            paymentId,
            purchaseId,
            status?.ToString() ?? NotFoundStatus);
}
