namespace PaymentProviderSim.Contracts.V1;
public sealed record PaymentStatusReported
{
    public required Guid PurchaseId { get; init; }
    public required Guid PaymentId { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset? AuthorizedAt { get; init; }
    public required DateTimeOffset? CapturedAt { get; init; }
    public required DateTimeOffset? VoidedAt { get; init; }
    public required DateTimeOffset? RefundedAt { get; init; }
    public required string? FailureReason { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
}