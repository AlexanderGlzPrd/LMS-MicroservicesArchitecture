namespace Enrollments.Contracts.V1;
public sealed record StudentEnrolled
{
    public required Guid StudentId { get; init; }
    public required Guid CourseId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
}