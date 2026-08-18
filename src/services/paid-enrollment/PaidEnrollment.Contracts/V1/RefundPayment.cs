namespace PaidEnrollment.Contracts.V1;
public sealed record RefundPayment
{
    public required Guid PurchaseId { get; init; }
    public required Guid PaymentId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
}