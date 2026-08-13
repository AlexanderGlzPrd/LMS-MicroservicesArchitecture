using Learning.Domain.Progress;
namespace Learning.Application.Abstractions.Exceptions;

public sealed class CourseNotAvailableException(CourseId courseId)
    : Exception($"El curso '{courseId.Value}' no esta disponible en el catalogo.")
{
    public CourseId CourseId { get; } = courseId;
}
