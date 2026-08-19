using PaidEnrollment.Application.Abstractions;
using PaidEnrollment.Domain.Purchases;
using PaidEnrollment.Domain.Purchases.Exceptions;
namespace PaidEnrollment.Application.Purchases.Workflow;
public sealed class PurchaseWorkflow(
    IPurchaseRepository purchases,
    IInbox inbox,
    IOutbox outbox,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public Task<ReplyReaction> OnPaymentAuthorizedAsync(
        SagaReply reply,
        DateTimeOffset authorizedAt,
        CancellationToken cancellationToken) =>
        ApplyAsync(
            reply,
            new PaymentEvidence(AuthorizedAt: authorizedAt),
            (purchase, now) => purchase.ConfirmAuthorization(authorizedAt, now),
            cancellationToken);

    public Task<ReplyReaction> OnPaymentDeclinedAsync(
        SagaReply reply,
        CancellationToken cancellationToken) =>
        ApplyAsync(
            reply,
            PaymentEvidence.None,
            (purchase, now) => purchase.RejectAsPaymentDeclined(now),
            cancellationToken);

    public Task<ReplyReaction> OnPaymentCapturedAsync(
        SagaReply reply,
        DateTimeOffset capturedAt,
        CancellationToken cancellationToken) =>
        ApplyAsync(
            reply,
            new PaymentEvidence(CapturedAt: capturedAt),
            (purchase, now) => purchase.ConfirmCapture(capturedAt, now),
            cancellationToken);

    public Task<ReplyReaction> OnCaptureFailedAsync(
        SagaReply reply,
        CancellationToken cancellationToken) =>
        ApplyAsync(
            reply,
            PaymentEvidence.None,
            (purchase, now) =>
            {
                purchase.BeginCompensationAfterCaptureFailure(now);
                EmitCompensation(purchase, now);
            },
            cancellationToken);

    public Task<ReplyReaction> OnAuthorizationVoidedAsync(
        SagaReply reply,
        DateTimeOffset voidedAt,
        CancellationToken cancellationToken) =>
        ApplyAsync(
            reply,
            new PaymentEvidence(VoidedAt: voidedAt),
            (purchase, now) =>
                purchase.CompleteCompensation(PurchaseReason.AuthorizationVoided, now),
            cancellationToken);

    public Task<ReplyReaction> OnPaymentRefundedAsync(
        SagaReply reply,
        DateTimeOffset refundedAt,
        CancellationToken cancellationToken) =>
        ApplyAsync(
            reply,
            new PaymentEvidence(RefundedAt: refundedAt),
            (purchase, now) => purchase.CompleteCompensation(PurchaseReason.PaymentRefunded, now),
            cancellationToken);

    public Task<ReplyReaction> OnRefundFailedAsync(
        SagaReply reply,
        CancellationToken cancellationToken) =>
        ApplyAsync(
            reply,
            PaymentEvidence.None,
            (purchase, now) => purchase.SuspendForReview(PurchaseReason.RefundFailed, now),
            cancellationToken);

    public Task<ReplyReaction> OnPaymentStatusReportedAsync(
        SagaReply reply,
        PaymentOutcome outcome,
        PaymentEvidence evidence,
        CancellationToken cancellationToken) =>
        ApplyAsync(
            reply,
            evidence,
            (purchase, now) => ApplyReportedStatus(purchase, outcome, now),
            cancellationToken);

    public Task<ReplyReaction> OnEnrollmentGrantedAsync(
        EnrollmentReply reply,
        GrantOutcome outcome,
        CancellationToken cancellationToken) =>
        ApplyEnrollmentAsync(
            reply,
            outcome,
            (purchase, now) =>
            {
                if (outcome is GrantOutcome.AlreadyExistedOther)
                {
                    purchase.SuspendForAccessFromAnotherOrigin(now);
                }
                else
                {
                    purchase.ConfirmEnrollmentGranted(outcome, now);
                }
            },
            cancellationToken);

    public Task<ReplyReaction> OnEnrollmentRejectedAsync(
        EnrollmentReply reply,
        CancellationToken cancellationToken) =>
        ApplyEnrollmentAsync(
            reply,
            GrantOutcome.Rejected,
            (purchase, now) =>
            {
                purchase.BeginCompensationAfterEnrollmentRejected(now);
                EmitCompensation(purchase, now);
            },
            cancellationToken);

    private void EmitCompensation(Purchase purchase, DateTimeOffset now)
    {
        if (purchase.CapturedAt is null)
        {
            outbox.EnqueueVoidAuthorization(purchase, now);
        }
        else
        {
            outbox.EnqueueRefundPayment(purchase, now);
        }

        purchase.RegisterStepAttempt(now);
    }

    private void ApplyReportedStatus(
        Purchase purchase,
        PaymentOutcome outcome,
        DateTimeOffset now)
    {
        switch (purchase.Status)
        {
            case PurchaseStatus.VerifyingAuthorizationOutcome:
                ApplyAuthorizationOutcome(purchase, outcome, now);

                break;

            case PurchaseStatus.VerifyingCaptureOutcome:
                ApplyCaptureOutcome(purchase, outcome, now);

                break;

            case PurchaseStatus.Compensating:
                ApplyCompensationOutcome(purchase, outcome, now);

                break;

            default:
                throw new InvalidPurchaseTransitionException(
                    purchase.Id, purchase.Status, $"PaymentStatusReported({outcome})");
        }
    }

    private static void ApplyAuthorizationOutcome(
        Purchase purchase,
        PaymentOutcome outcome,
        DateTimeOffset now)
    {
        switch (outcome)
        {
            case PaymentOutcome.Authorized:
                purchase.ConfirmAuthorization(purchase.AuthorizedAt ?? now, now);

                break;

            case PaymentOutcome.Declined:
                purchase.RejectAsPaymentDeclined(now);

                break;

            case PaymentOutcome.NotFound:
                purchase.RejectAsAuthorizationNotFound(now);

                break;

            case PaymentOutcome.Captured:
                purchase.ConfirmCapture(purchase.CapturedAt ?? now, now);

                break;

            default:
                throw new InvalidPurchaseTransitionException(
                    purchase.Id, purchase.Status, $"PaymentStatusReported({outcome})");
        }
    }

    private void ApplyCaptureOutcome(
        Purchase purchase,
        PaymentOutcome outcome,
        DateTimeOffset now)
    {
        switch (outcome)
        {
            case PaymentOutcome.Captured:
                purchase.ConfirmCapture(purchase.CapturedAt ?? now, now);

                break;

            case PaymentOutcome.Authorized:
            case PaymentOutcome.CaptureFailed:
                purchase.BeginCompensationAfterCaptureFailure(now);
                EmitCompensation(purchase, now);

                break;

            default:
                purchase.SuspendForReview(PurchaseReason.CaptureOutcomeInconsistent, now);

                break;
        }
    }

    private void ApplyCompensationOutcome(
        Purchase purchase,
        PaymentOutcome outcome,
        DateTimeOffset now)
    {
        switch (outcome)
        {
            case PaymentOutcome.Voided:
                purchase.CompleteCompensation(PurchaseReason.AuthorizationVoided, now);

                break;

            case PaymentOutcome.Refunded:
                purchase.CompleteCompensation(PurchaseReason.PaymentRefunded, now);

                break;

            case PaymentOutcome.Authorized:
            case PaymentOutcome.CaptureFailed:
            case PaymentOutcome.Captured:
                EmitCompensation(purchase, now);

                break;

            default:
                throw new InvalidPurchaseTransitionException(
                    purchase.Id, purchase.Status, $"PaymentStatusReported({outcome})");
        }
    }

    private Task<ReplyReaction> ApplyEnrollmentAsync(
        EnrollmentReply reply,
        GrantOutcome outcome,
        Action<Purchase, DateTimeOffset> transition,
        CancellationToken cancellationToken) =>
        ApplyCoreAsync(
            reply.MessageId,
            reply.MessageType,
            reply.PurchaseId,
            purchase => purchase.StudentId == reply.StudentId
                && purchase.CourseId == reply.CourseId,
            PaymentEvidence.None,
            outcome,
            transition,
            cancellationToken);

    private Task<ReplyReaction> ApplyAsync(
        SagaReply reply,
        PaymentEvidence evidence,
        Action<Purchase, DateTimeOffset> transition,
        CancellationToken cancellationToken) =>
        ApplyCoreAsync(
            reply.MessageId,
            reply.MessageType,
            reply.PurchaseId,
            purchase => purchase.PaymentId == reply.PaymentId,
            evidence,
            grantOutcome: null,
            transition,
            cancellationToken);

    private async Task<ReplyReaction> ApplyCoreAsync(
        Guid messageId,
        string messageType,
        PurchaseId purchaseId,
        Func<Purchase, bool> isCorrelated,
        PaymentEvidence evidence,
        GrantOutcome? grantOutcome,
        Action<Purchase, DateTimeOffset> transition,
        CancellationToken cancellationToken)
    {
        var purchase = await purchases.FindAsync(purchaseId, cancellationToken);

        if (purchase is null || !isCorrelated(purchase))
        {
            return ReplyReaction.CorrelationMismatch;
        }

        if (await inbox.HasBeenProcessedAsync(messageId, cancellationToken))
        {
            return ReplyReaction.AlreadyProcessed;
        }

        var now = timeProvider.GetUtcNow();
        var reaction = await RecordAsync(
            purchase, evidence, grantOutcome, transition, messageId, messageType, now,
            cancellationToken);

        return reaction;
    }

    private async Task<ReplyReaction> RecordAsync(
        Purchase purchase,
        PaymentEvidence evidence,
        GrantOutcome? grantOutcome,
        Action<Purchase, DateTimeOffset> transition,
        Guid messageId,
        string messageType,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var reaction = ReplyReaction.Applied;

        if (purchase.IsTerminal)
        {
            reaction = ReplyReaction.Late;
        }
        else if (purchase.IsUnderReview)
        {
            Restore(purchase, evidence, grantOutcome, now);

            reaction = ReplyReaction.EvidenceOnly;
        }
        else
        {
            Restore(purchase, evidence, grantOutcome, now);

            try
            {
                transition(purchase, now);
            }
            catch (InvalidPurchaseTransitionException)
            {
                reaction = ReplyReaction.NotApplicable;
            }
        }

        inbox.Record(messageId, messageType, now);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return reaction;
    }

    private static void Restore(
        Purchase purchase,
        PaymentEvidence evidence,
        GrantOutcome? grantOutcome,
        DateTimeOffset now)
    {
        purchase.RestoreEvidence(
            evidence.AuthorizedAt,
            evidence.CapturedAt,
            evidence.VoidedAt,
            evidence.RefundedAt,
            now);

        if (grantOutcome is not null)
        {
            purchase.RegisterGrantOutcome(grantOutcome.Value, now);
        }
    }
}
