namespace Learning.Infrastructure.Projection;
internal sealed class ProgressEvent
{
    public required Guid Id { get; init; }

    public long SequenceNo { get; init; }

    public required Guid StudentId { get; init; }

    public required Guid CourseId { get; init; }

    public required string EventType { get; init; }

    public required string Payload { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    public DateTimeOffset? AppliedAt { get; set; }

    public int AttemptCount { get; set; }

    public string? LastError { get; set; }

    public DateTimeOffset? LastAttemptAt { get; set; }
}
