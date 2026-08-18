using PaidEnrollment.Domain.Abstractions;
namespace PaidEnrollment.Domain.Purchases.Exceptions;
public sealed class InvalidPurchaseTransitionException(
    PurchaseId purchaseId,
    PurchaseStatus status,
    string transition)
    : DomainException(
        $"La compra '{purchaseId.Value}' esta en '{status}' y no admite la transicion "
        + $"'{transition}'.")
{
    public PurchaseId PurchaseId { get; } = purchaseId;

    public PurchaseStatus Status { get; } = status;

    public string Transition { get; } = transition;
}
