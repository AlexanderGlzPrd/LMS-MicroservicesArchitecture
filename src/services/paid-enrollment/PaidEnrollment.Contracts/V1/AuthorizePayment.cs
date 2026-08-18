namespace PaidEnrollment.Contracts.V1;
public sealed record AuthorizePayment
{
    public required Guid PurchaseId { get; init; }
    public required Guid PaymentId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
}