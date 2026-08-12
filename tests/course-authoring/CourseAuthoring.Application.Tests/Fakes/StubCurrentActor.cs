using CourseAuthoring.Application.Abstractions;
using CourseAuthoring.Domain.Courses;

namespace CourseAuthoring.Application.Tests.Fakes;

internal sealed class StubCurrentActor(InstructorId instructorId) : ICurrentActor
{
    public InstructorId InstructorId { get; } = instructorId;
}
