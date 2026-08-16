using Learning.Domain.Abstractions;
namespace Learning.Domain.Progress.Events;
public sealed record LessonCompleted(
    Guid LessonId,
    DateTimeOffset OccurredAt,
    int ObservedTotalLessonCount) : IDomainEvent;