using PaidEnrollment.Domain.Purchases.Exceptions;
namespace PaidEnrollment.Domain.Purchases;
public sealed class PurchaseResolution
{
    public const int MaxEvidenceLength = 500;

    private PurchaseResolution()
    {
    }

    public static PurchaseResolution Record(
        Guid id,
        PurchaseId purchaseId,
        ManualResolution resolution,
        string evidence,
        Guid operatorId,
        DateTimeOffset resolvedAt)
    {
        if (operatorId == Guid.Empty)
        {
            throw new InvalidPurchaseIdentityException(nameof(operatorId));
        }

        return new PurchaseResolution
        {
            Id = id,
            PurchaseId = purchaseId,
            Resolution = resolution,
            Evidence = evidence,
            OperatorId = operatorId,
            ResolvedAt = resolvedAt,
        };
    }

    public Guid Id { get; private set; }

    public PurchaseId PurchaseId { get; private set; }

    public ManualResolution Resolution { get; private set; }

    public string Evidence { get; private set; } = string.Empty;

    public Guid OperatorId { get; private set; }

    public DateTimeOffset ResolvedAt { get; private set; }
}