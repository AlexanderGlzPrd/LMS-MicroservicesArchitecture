namespace Learning.Contracts.V1;
public sealed record CourseCompleted
{
    public required Guid StudentId { get; init; }
    public required Guid CourseId { get; init; }
    public required DateTimeOffset CompletedAt { get; init; }
}