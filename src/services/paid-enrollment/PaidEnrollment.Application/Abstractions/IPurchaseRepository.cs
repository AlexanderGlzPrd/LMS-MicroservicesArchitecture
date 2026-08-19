using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Application.Abstractions;
public interface IPurchaseRepository
{
    Task<Purchase?> FindAsync(PurchaseId purchaseId, CancellationToken cancellationToken);
    Task<Purchase?> FindBlockingAsync(
        StudentId studentId,
        CourseId courseId,
        CancellationToken cancellationToken);

    // Los estados que el driver hace avanzar por si solo, sin esperar respuesta de nadie.
    Task<IReadOnlyList<Purchase>> ListDrivableAsync(int batchSize, CancellationToken cancellationToken);

    // Los estados que esperan una respuesta y llevan vencido el paso: hay que reconciliarlos.
    Task<IReadOnlyList<Purchase>> ListExpiredAsync(
        DateTimeOffset expiredBefore,
        int batchSize,
        CancellationToken cancellationToken);

    void Add(Purchase purchase);

    void AddResolution(PurchaseResolution resolution);
}