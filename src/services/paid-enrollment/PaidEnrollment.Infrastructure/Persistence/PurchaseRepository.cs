using Microsoft.EntityFrameworkCore;
using PaidEnrollment.Application.Abstractions;
using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Infrastructure.Persistence;
internal sealed class PurchaseRepository(PaidEnrollmentDbContext context) : IPurchaseRepository
{
    private static readonly PurchaseStatus[] Freeing =
    [
        PurchaseStatus.Confirmed,
        PurchaseStatus.Rejected,
        PurchaseStatus.Compensated,
    ];

    private static readonly PurchaseStatus[] Drivable =
    [
        PurchaseStatus.Started,
        PurchaseStatus.CheckingAccess,
        PurchaseStatus.PaymentAuthorized,
        PurchaseStatus.PaymentCaptured,
        PurchaseStatus.EnrollmentGranted,
        PurchaseStatus.Compensating,
    ];

    private static readonly PurchaseStatus[] Reconcilable =
    [
        PurchaseStatus.AuthorizingPayment,
        PurchaseStatus.CapturingPayment,
        PurchaseStatus.GrantingEnrollment,
        PurchaseStatus.VerifyingAuthorizationOutcome,
        PurchaseStatus.VerifyingCaptureOutcome,
        PurchaseStatus.VerifyingEnrollmentOutcome,
        PurchaseStatus.Compensating,
    ];

    public Task<Purchase?> FindAsync(
        PurchaseId purchaseId,
        CancellationToken cancellationToken) =>
        context.Purchases.FirstOrDefaultAsync(
            purchase => purchase.Id == purchaseId, cancellationToken);

    public Task<Purchase?> FindBlockingAsync(
        StudentId studentId,
        CourseId courseId,
        CancellationToken cancellationToken) =>
        context.Purchases.FirstOrDefaultAsync(
            purchase => purchase.StudentId == studentId
                && purchase.CourseId == courseId
                && !Freeing.Contains(purchase.Status),
            cancellationToken);

    public async Task<IReadOnlyList<Purchase>> ListDrivableAsync(
        int batchSize,
        CancellationToken cancellationToken) =>
        await context.Purchases
            .Where(purchase => Drivable.Contains(purchase.Status))
            .OrderBy(purchase => purchase.StepStartedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Purchase>> ListExpiredAsync(
        DateTimeOffset expiredBefore,
        int batchSize,
        CancellationToken cancellationToken) =>
        await context.Purchases
            .Where(purchase => Reconcilable.Contains(purchase.Status)
                && purchase.StepStartedAt <= expiredBefore)
            .OrderBy(purchase => purchase.StepStartedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public void Add(Purchase purchase) => context.Purchases.Add(purchase);

    public void AddResolution(PurchaseResolution resolution) =>
        context.PurchaseResolutions.Add(resolution);
}