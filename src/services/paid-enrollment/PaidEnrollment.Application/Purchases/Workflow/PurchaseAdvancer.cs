using PaidEnrollment.Application.Abstractions;
using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Application.Purchases.Workflow;
public sealed class PurchaseAdvancer(
    IUnitOfWork unitOfWork,
    IOutbox outbox,
    IEnrollmentAccess enrollmentAccess,
    TimeProvider timeProvider)
{
    public async Task<PurchaseStatus> AdvanceAsync(
        Purchase purchase,
        int maxPreCheckAttempts,
        CancellationToken cancellationToken)
    {
        switch (purchase.Status)
        {
            case PurchaseStatus.Started:
                purchase.BeginAccessCheck(timeProvider.GetUtcNow());

                break;

            case PurchaseStatus.CheckingAccess:
                await CheckAccessAsync(purchase, maxPreCheckAttempts, cancellationToken);

                break;

            case PurchaseStatus.PaymentAuthorized:
                purchase.BeginCapture(timeProvider.GetUtcNow());
                outbox.EnqueueCapturePayment(purchase, purchase.UpdatedAt);

                break;

            case PurchaseStatus.PaymentCaptured:
                purchase.BeginEnrollmentGrant(timeProvider.GetUtcNow());
                outbox.EnqueueGrantEnrollmentForCapturedPayment(purchase, purchase.UpdatedAt);

                break;

            case PurchaseStatus.EnrollmentGranted:
                purchase.Confirm(timeProvider.GetUtcNow());

                break;

            case PurchaseStatus.Compensating when purchase.StepAttempts == 0:
                EmitCompensation(purchase, timeProvider.GetUtcNow());

                break;

            default:
                return purchase.Status;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return purchase.Status;
    }

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

    private async Task CheckAccessAsync(
        Purchase purchase,
        int maxPreCheckAttempts,
        CancellationToken cancellationToken)
    {
        var access = await enrollmentAccess.CheckAsync(
            purchase.StudentId, purchase.CourseId, cancellationToken);

        var now = timeProvider.GetUtcNow();

        switch (access)
        {
            case EnrollmentAccess.Enrolled:
                purchase.RejectAsAlreadyEnrolled(now);

                break;

            case EnrollmentAccess.NotEnrolled:
                purchase.BeginAuthorization(now);
                outbox.EnqueueAuthorizePayment(purchase, now);

                break;

            default:
                purchase.RegisterStepAttempt(now);

                if (purchase.StepAttempts >= maxPreCheckAttempts)
                {
                    purchase.RejectAsPreCheckUnavailable(now);
                }

                break;
        }
    }
}
