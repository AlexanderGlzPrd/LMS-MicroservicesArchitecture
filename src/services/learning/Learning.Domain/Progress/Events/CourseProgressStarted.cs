using Learning.Domain.Abstractions;
namespace Learning.Domain.Progress.Events;
public sealed record CourseProgressStarted(DateTimeOffset OccurredAt) : IDomainEvent;