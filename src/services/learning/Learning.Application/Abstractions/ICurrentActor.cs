using Learning.Domain.Progress;

namespace Learning.Application.Abstractions;

public interface ICurrentActor
{
    StudentId StudentId { get; }
}
