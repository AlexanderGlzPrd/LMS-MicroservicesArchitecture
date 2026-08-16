namespace Learning.Domain.Abstractions;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
