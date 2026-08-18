namespace PaymentProviderSim.Contracts.V1;
public sealed record AuthorizationVoided
{
    public required Guid PurchaseId { get; init; }
    public required Guid PaymentId { get; init; }
    public required DateTimeOffset VoidedAt { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
}