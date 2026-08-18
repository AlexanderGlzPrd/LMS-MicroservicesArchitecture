using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Application.Abstractions;
public interface IPurchaseRepository
{
    Task<Purchase?> FindAsync(PurchaseId purchaseId, CancellationToken cancellationToken);
    Task<Purchase?> FindBlockingAsync(
        StudentId studentId,
        CourseId courseId,
        CancellationToken cancellationToken);

    void Add(Purchase purchase);

    void AddResolution(PurchaseResolution resolution);
}