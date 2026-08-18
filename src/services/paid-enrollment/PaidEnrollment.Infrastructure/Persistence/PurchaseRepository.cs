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

    public void Add(Purchase purchase) => context.Purchases.Add(purchase);

    public void AddResolution(PurchaseResolution resolution) =>
        context.PurchaseResolutions.Add(resolution);
}