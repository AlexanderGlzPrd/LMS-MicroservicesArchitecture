using PaidEnrollment.Application.Abstractions;
using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Application.Purchases.Workflow;
public sealed class PurchaseReconciliation(
    IUnitOfWork unitOfWork,
    IOutbox outbox,
    TimeProvider timeProvider)
{
    public async Task<PurchaseStatus> ReconcileAsync(
        Purchase purchase,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        switch (purchase.Status)
        {
            case PurchaseStatus.AuthorizingPayment:
                purchase.BeginAuthorizationVerification(now);
                outbox.EnqueueGetPaymentStatus(purchase, now);

                break;

            case PurchaseStatus.CapturingPayment:
                purchase.BeginCaptureVerification(now);
                outbox.EnqueueGetPaymentStatus(purchase, now);

                break;

            case PurchaseStatus.GrantingEnrollment:
                purchase.BeginEnrollmentVerification(now);
                outbox.EnqueueGrantEnrollmentForCapturedPayment(purchase, now);

                break;

            case PurchaseStatus.VerifyingAuthorizationOutcome:
                Query(purchase, maxAttempts, PurchaseReason.AuthorizationOutcomeUnknown, now);

                break;

            case PurchaseStatus.VerifyingCaptureOutcome:
                Query(purchase, maxAttempts, PurchaseReason.CaptureOutcomeUnknown, now);

                break;

            case PurchaseStatus.VerifyingEnrollmentOutcome:
                Resend(purchase, maxAttempts, now);

                break;

            case PurchaseStatus.Compensating:
                QueryCompensation(purchase, maxAttempts, now);

                break;

            default:
                return purchase.Status;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return purchase.Status;
    }

    private void Query(
        Purchase purchase,
        int maxAttempts,
        PurchaseReason exhausted,
        DateTimeOffset now)
    {
        if (purchase.StepAttempts + 1 >= maxAttempts)
        {
            purchase.SuspendForReview(exhausted, now);

            return;
        }

        outbox.EnqueueGetPaymentStatus(purchase, now);
        purchase.RegisterStepAttempt(now);
    }

    private void Resend(Purchase purchase, int maxAttempts, DateTimeOffset now)
    {
        if (purchase.StepAttempts + 1 >= maxAttempts)
        {
            purchase.SuspendForReview(PurchaseReason.EnrollmentOutcomeUnknown, now);

            return;
        }

        outbox.EnqueueGrantEnrollmentForCapturedPayment(purchase, now);
        purchase.RegisterStepAttempt(now);
    }

    private void QueryCompensation(Purchase purchase, int maxAttempts, DateTimeOffset now)
    {
        if (purchase.StepAttempts - 1 >= maxAttempts)
        {
            purchase.SuspendForReview(PurchaseReason.CompensationOutcomeUnknown, now);

            return;
        }

        outbox.EnqueueGetPaymentStatus(purchase, now);
        purchase.RegisterStepAttempt(now);
    }
}
