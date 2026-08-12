using CourseAuthoring.Domain.Abstractions;

namespace CourseAuthoring.Domain.Courses.Exceptions;

public sealed class CourseHasNoLessonsException(CourseId courseId)
    : DomainException(
        $"El curso '{courseId.Value}' no tiene lecciones de trabajo: no se puede publicar ni republicar.")
{
    public CourseId CourseId { get; } = courseId;
}
