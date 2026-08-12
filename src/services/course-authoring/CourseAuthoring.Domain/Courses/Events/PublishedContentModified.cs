using CourseAuthoring.Domain.Abstractions;

namespace CourseAuthoring.Domain.Courses.Events;

public sealed record PublishedContentModified(
    CourseId CourseId,
    DateTimeOffset OccurredAt) : IDomainEvent;
