using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Application.Abstractions.Exceptions;
public sealed class PurchaseNotFoundException(PurchaseId purchaseId)
    : Exception($"No existe una compra '{purchaseId.Value}' para este estudiante.")
{
    public PurchaseId PurchaseId { get; } = purchaseId;
}
