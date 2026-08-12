namespace CourseAuthoring.Domain.Abstractions;
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
