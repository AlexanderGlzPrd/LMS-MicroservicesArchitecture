using CourseAuthoring.Domain.Abstractions;

namespace CourseAuthoring.Domain.Courses.Events;

public sealed record CoursePublished(
    CourseId CourseId,
    InstructorId InstructorId,
    DateTimeOffset OccurredAt) : IDomainEvent;
