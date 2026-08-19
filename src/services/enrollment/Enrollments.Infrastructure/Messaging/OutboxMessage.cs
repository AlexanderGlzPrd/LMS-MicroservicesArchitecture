namespace Enrollments.Infrastructure.Messaging;
internal sealed class OutboxMessage
{
    public required Guid Id { get; init; }

    public required Guid AggregateId { get; init; }

    public required string MessageType { get; init; }

    public string? RoutingKey { get; init; }

    public required string Payload { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    public DateTimeOffset? PublishedAt { get; set; }

    public int AttemptCount { get; set; }

    public string? LastError { get; set; }

    public DateTimeOffset? LastAttemptAt { get; set; }

    public string? TraceContext { get; init; }
}