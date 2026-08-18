namespace PaymentProviderSim.Contracts.V1;
public sealed record PaymentRefunded
{
    public required Guid PurchaseId { get; init; }
    public required Guid PaymentId { get; init; }
    public required DateTimeOffset RefundedAt { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
}