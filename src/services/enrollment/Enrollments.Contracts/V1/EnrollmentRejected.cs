namespace Enrollments.Contracts.V1;
public sealed record EnrollmentRejected
{
    public required Guid PurchaseId { get; init; }
    public required Guid StudentId { get; init; }
    public required Guid CourseId { get; init; }
    public required string Reason { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
}