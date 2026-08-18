namespace PaymentProviderSim.Contracts.V1;
public sealed record PaymentAuthorized
{
    public required Guid PurchaseId { get; init; }
    public required Guid PaymentId { get; init; }
    public required DateTimeOffset AuthorizedAt { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
}