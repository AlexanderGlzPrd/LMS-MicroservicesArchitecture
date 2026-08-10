using CourseAuthoring.Domain.Courses;
namespace CourseAuthoring.Application.Abstractions;
public interface ICurrentActor
{
    InstructorId InstructorId { get; }
}
