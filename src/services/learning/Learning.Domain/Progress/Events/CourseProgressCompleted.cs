using Learning.Domain.Abstractions;
namespace Learning.Domain.Progress.Events;
public sealed record CourseProgressCompleted(
    DateTimeOffset OccurredAt,
    int ObservedTotalLessonCount) : IDomainEvent;