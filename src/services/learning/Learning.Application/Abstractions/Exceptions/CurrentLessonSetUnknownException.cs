using Learning.Domain.Progress;
namespace Learning.Application.Abstractions.Exceptions;

public sealed class CurrentLessonSetUnknownException(CourseId courseId)
    : Exception($"No se ha podido obtener el contenido publicado del curso '{courseId.Value}'.")
{
    public CourseId CourseId { get; } = courseId;
}
