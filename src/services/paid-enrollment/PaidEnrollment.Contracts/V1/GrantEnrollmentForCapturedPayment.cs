namespace PaidEnrollment.Contracts.V1;
public sealed record GrantEnrollmentForCapturedPayment
{
    public required Guid PurchaseId { get; init; }
    public required Guid StudentId { get; init; }
    public required Guid CourseId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
}