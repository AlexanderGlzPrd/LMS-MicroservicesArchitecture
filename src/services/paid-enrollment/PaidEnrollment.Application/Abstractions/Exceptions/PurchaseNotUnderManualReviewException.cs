using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Application.Abstractions.Exceptions;
public sealed class PurchaseNotUnderManualReviewException(
    PurchaseId purchaseId,
    PurchaseStatus status)
    : Exception(
        $"La compra '{purchaseId.Value}' esta en '{status}' y no admite una resolucion manual.")
{
    public PurchaseId PurchaseId { get; } = purchaseId;

    public PurchaseStatus Status { get; } = status;
}
