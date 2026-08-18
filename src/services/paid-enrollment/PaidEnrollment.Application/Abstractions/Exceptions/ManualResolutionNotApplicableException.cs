using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Application.Abstractions.Exceptions;
public sealed class ManualResolutionNotApplicableException(
    PurchaseId purchaseId,
    ManualResolution resolution,
    string precondition)
    : Exception(
        $"La resolucion '{resolution}' de la compra '{purchaseId.Value}' no aplica: "
        + $"{precondition}")
{
    public PurchaseId PurchaseId { get; } = purchaseId;

    public ManualResolution Resolution { get; } = resolution;

    public string Precondition { get; } = precondition;
}
