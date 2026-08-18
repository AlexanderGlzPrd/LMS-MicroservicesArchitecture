namespace Enrollments.Application.Abstractions;
public interface IPurchaseGrantLedger
{
    Task<PurchaseGrantEntry?> FindAsync(PurchaseId purchaseId, CancellationToken cancellationToken);
    void Add(PurchaseGrantEntry entry);
}