namespace PaymentProviderSim.Contracts.V1;
public sealed record RefundFailed
{
    public required Guid PurchaseId { get; init; }
    public required Guid PaymentId { get; init; }
    public required string Reason { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
}