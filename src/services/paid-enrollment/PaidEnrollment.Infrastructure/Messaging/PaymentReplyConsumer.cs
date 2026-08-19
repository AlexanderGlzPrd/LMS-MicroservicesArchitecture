using MassTransit;
using Microsoft.Extensions.Logging;
using PaidEnrollment.Application.Purchases.Workflow;
using PaidEnrollment.Domain.Purchases;
using PaymentProviderSim.Contracts.V1;
namespace PaidEnrollment.Infrastructure.Messaging;
internal sealed class PaymentReplyConsumer(
    PurchaseWorkflow workflow,
    ILogger<PaymentReplyConsumer> logger) :
    IConsumer<PaymentAuthorized>,
    IConsumer<PaymentDeclined>,
    IConsumer<PaymentCaptured>,
    IConsumer<CaptureFailed>,
    IConsumer<AuthorizationVoided>,
    IConsumer<PaymentRefunded>,
    IConsumer<RefundFailed>,
    IConsumer<PaymentStatusReported>
{
    private static readonly EventId CorrelationMismatchEvent =
        new(8001, "saga-correlation-mismatch");

    private static readonly EventId LateMessageEvent = new(8002, "saga-late-message");

    public async Task Consume(ConsumeContext<PaymentAuthorized> context)
    {
        var message = context.Message;
        var reply = Envelope<PaymentAuthorized>(
            context, message.PurchaseId, message.PaymentId, message.OccurredAt);

        if (message.AuthorizedAt == default)
        {
            throw new InvalidSagaReplyMessageException(
                nameof(PaymentAuthorized), "AuthorizedAt no tiene valor.");
        }

        Report(
            reply,
            await workflow.OnPaymentAuthorizedAsync(
                reply, message.AuthorizedAt, context.CancellationToken));
    }

    public async Task Consume(ConsumeContext<PaymentDeclined> context)
    {
        var message = context.Message;
        var reply = Envelope<PaymentDeclined>(
            context, message.PurchaseId, message.PaymentId, message.OccurredAt);

        Report(reply, await workflow.OnPaymentDeclinedAsync(reply, context.CancellationToken));
    }

    public async Task Consume(ConsumeContext<PaymentCaptured> context)
    {
        var message = context.Message;
        var reply = Envelope<PaymentCaptured>(
            context, message.PurchaseId, message.PaymentId, message.OccurredAt);

        if (message.CapturedAt == default)
        {
            throw new InvalidSagaReplyMessageException(
                nameof(PaymentCaptured), "CapturedAt no tiene valor.");
        }

        Report(
            reply,
            await workflow.OnPaymentCapturedAsync(
                reply, message.CapturedAt, context.CancellationToken));
    }

    public async Task Consume(ConsumeContext<CaptureFailed> context)
    {
        var message = context.Message;
        var reply = Envelope<CaptureFailed>(
            context, message.PurchaseId, message.PaymentId, message.OccurredAt);

        Report(reply, await workflow.OnCaptureFailedAsync(reply, context.CancellationToken));
    }

    public async Task Consume(ConsumeContext<AuthorizationVoided> context)
    {
        var message = context.Message;
        var reply = Envelope<AuthorizationVoided>(
            context, message.PurchaseId, message.PaymentId, message.OccurredAt);

        if (message.VoidedAt == default)
        {
            throw new InvalidSagaReplyMessageException(
                nameof(AuthorizationVoided), "VoidedAt no tiene valor.");
        }

        Report(
            reply,
            await workflow.OnAuthorizationVoidedAsync(
                reply, message.VoidedAt, context.CancellationToken));
    }

    public async Task Consume(ConsumeContext<PaymentRefunded> context)
    {
        var message = context.Message;
        var reply = Envelope<PaymentRefunded>(
            context, message.PurchaseId, message.PaymentId, message.OccurredAt);

        if (message.RefundedAt == default)
        {
            throw new InvalidSagaReplyMessageException(
                nameof(PaymentRefunded), "RefundedAt no tiene valor.");
        }

        Report(
            reply,
            await workflow.OnPaymentRefundedAsync(
                reply, message.RefundedAt, context.CancellationToken));
    }

    public async Task Consume(ConsumeContext<RefundFailed> context)
    {
        var message = context.Message;
        var reply = Envelope<RefundFailed>(
            context, message.PurchaseId, message.PaymentId, message.OccurredAt);

        Report(reply, await workflow.OnRefundFailedAsync(reply, context.CancellationToken));
    }

    public async Task Consume(ConsumeContext<PaymentStatusReported> context)
    {
        var message = context.Message;
        var reply = Envelope<PaymentStatusReported>(
            context, message.PurchaseId, message.PaymentId, message.OccurredAt);

        var outcome = ParseOutcome(message.Status);

        EnsureEvidenceMatchesStatus(outcome, message);

        var evidence = new PaymentEvidence(
            message.AuthorizedAt, message.CapturedAt, message.VoidedAt, message.RefundedAt);

        Report(
            reply,
            await workflow.OnPaymentStatusReportedAsync(
                reply, outcome, evidence, context.CancellationToken));
    }

    private static PaymentOutcome ParseOutcome(string status) =>
        Enum.TryParse<PaymentOutcome>(status, out var outcome)
            ? outcome
            : throw new InvalidSagaReplyMessageException(
                nameof(PaymentStatusReported), $"Status '{status}' no es reconocible.");

    private static void EnsureEvidenceMatchesStatus(
        PaymentOutcome outcome,
        PaymentStatusReported message)
    {
        var required = outcome switch
        {
            PaymentOutcome.Authorized => message.AuthorizedAt is not null,
            PaymentOutcome.Captured => message.AuthorizedAt is not null
                && message.CapturedAt is not null,
            PaymentOutcome.CaptureFailed => message.AuthorizedAt is not null
                && message.CapturedAt is null,
            PaymentOutcome.Voided => message.AuthorizedAt is not null
                && message.VoidedAt is not null,
            PaymentOutcome.Refunded => message.AuthorizedAt is not null
                && message.CapturedAt is not null
                && message.RefundedAt is not null,
            PaymentOutcome.NotFound or PaymentOutcome.Declined => message.AuthorizedAt is null
                && message.CapturedAt is null
                && message.VoidedAt is null
                && message.RefundedAt is null,

            _ => true,
        };

        if (!required)
        {
            throw new InvalidSagaReplyMessageException(
                nameof(PaymentStatusReported),
                $"el estado '{outcome}' no concuerda con las marcas temporales recibidas.");
        }
    }

    private void Report(SagaReply reply, ReplyReaction reaction)
    {
        switch (reaction)
        {
            case ReplyReaction.CorrelationMismatch:
                logger.LogError(
                    CorrelationMismatchEvent,
                    "La respuesta {MessageType} declara la compra {PurchaseId} y el pago "
                    + "{PaymentId}, que no se corresponden. No se aplica ni se deduplica nada.",
                    reply.MessageType,
                    reply.PurchaseId.Value,
                    reply.PaymentId.Value);

                throw new SagaCorrelationMismatchException(
                    reply.MessageType, reply.PurchaseId.Value);

            case ReplyReaction.Late:
            case ReplyReaction.NotApplicable:
            case ReplyReaction.EvidenceOnly:
                logger.LogInformation(
                    LateMessageEvent,
                    "La respuesta {MessageType} de la compra {PurchaseId} se registro sin "
                    + "avanzar la Saga: {Reaction}.",
                    reply.MessageType,
                    reply.PurchaseId.Value,
                    reaction);

                break;

            default:
                break;
        }
    }

    private static SagaReply Envelope<TContract>(
        ConsumeContext context,
        Guid purchaseId,
        Guid paymentId,
        DateTimeOffset occurredAt)
    {
        var name = typeof(TContract).Name;

        var messageId = context.MessageId
            ?? throw new InvalidSagaReplyMessageException(name, "el sobre no trae MessageId.");

        if (purchaseId == Guid.Empty)
        {
            throw new InvalidSagaReplyMessageException(name, "PurchaseId esta a ceros.");
        }

        if (paymentId == Guid.Empty)
        {
            throw new InvalidSagaReplyMessageException(name, "PaymentId esta a ceros.");
        }

        if (occurredAt == default)
        {
            throw new InvalidSagaReplyMessageException(name, "OccurredAt no tiene valor.");
        }

        return new SagaReply(
            messageId,
            typeof(TContract).FullName!,
            new PurchaseId(purchaseId),
            new PaymentId(paymentId));
    }
}
