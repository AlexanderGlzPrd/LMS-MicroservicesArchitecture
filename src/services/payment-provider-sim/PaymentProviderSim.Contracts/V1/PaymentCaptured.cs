namespace PaymentProviderSim.Contracts.V1;
public sealed record PaymentCaptured
{
    public required Guid PurchaseId { get; init; }
    public required Guid PaymentId { get; init; }
    public required DateTimeOffset CapturedAt { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
}