using Enrollments.Application.Abstractions;
using Enrollments.Domain.Enrollments;
namespace Enrollments.Application.Tests.Fakes;

internal sealed class StubCurrentActor(StudentId studentId) : ICurrentActor
{
    public StudentId StudentId { get; } = studentId;
}
