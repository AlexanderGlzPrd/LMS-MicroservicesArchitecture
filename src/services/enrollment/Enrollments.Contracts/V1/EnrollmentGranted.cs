namespace Enrollments.Contracts.V1;
public sealed record EnrollmentGranted
{
    public required Guid PurchaseId { get; init; }
    public required Guid StudentId { get; init; }
    public required Guid CourseId { get; init; }
    public required string Outcome { get; init; }
    public required string Origin { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
}