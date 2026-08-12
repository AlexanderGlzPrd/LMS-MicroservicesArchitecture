using CourseAuthoring.Domain.Abstractions;
namespace CourseAuthoring.Domain.Courses.Exceptions;
public sealed class CourseOwnershipException(CourseId courseId, InstructorId actor)
    : DomainException($"El instructor '{actor.Value}' no es el propietario del curso '{courseId.Value}'.")
{
    public CourseId CourseId { get; } = courseId;

    public InstructorId Actor { get; } = actor;
}
