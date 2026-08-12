using CourseAuthoring.Domain.Abstractions;

namespace CourseAuthoring.Domain.Courses.Exceptions;

public sealed class LessonNotFoundException(CourseId courseId, LessonId lessonId)
    : DomainException($"El curso '{courseId.Value}' no contiene la leccion '{lessonId.Value}'.")
{
    public CourseId CourseId { get; } = courseId;

    public LessonId LessonId { get; } = lessonId;
}
