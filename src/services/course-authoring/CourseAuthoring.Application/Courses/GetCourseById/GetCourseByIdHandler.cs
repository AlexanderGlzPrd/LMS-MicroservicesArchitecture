using CourseAuthoring.Application.Abstractions;
using CourseAuthoring.Domain.Courses.Exceptions;

namespace CourseAuthoring.Application.Courses.GetCourseById;

public sealed class GetCourseByIdHandler(ICourseRepository courses, ICurrentActor currentActor)
{
    /// <summary>
    /// Devuelve <c>null</c> si el curso no existe.
    /// </summary>
    /// <exception cref="CourseOwnershipException">Si el actor no es el propietario.</exception>
    public async Task<CourseView?> HandleAsync(
        GetCourseByIdQuery query,
        CancellationToken cancellationToken)
    {
        var course = await courses.GetByIdAsync(query.CourseId, cancellationToken);

        if (course is null)
        {
            return null;
        }

        if (course.InstructorId != currentActor.InstructorId)
        {
            throw new CourseOwnershipException(course.Id, currentActor.InstructorId);
        }

        return CourseView.From(course);
    }
}
