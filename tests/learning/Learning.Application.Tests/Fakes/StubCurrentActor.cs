using Learning.Application.Abstractions;
using Learning.Domain.Progress;

namespace Learning.Application.Tests.Fakes;

internal sealed class StubCurrentActor(StudentId studentId) : ICurrentActor
{
    public StudentId StudentId { get; } = studentId;
}
