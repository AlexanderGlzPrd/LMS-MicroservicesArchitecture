using Enrollments.Domain.Enrollments;

namespace Enrollments.Application.Abstractions.Exceptions;

// No se ha podido verificar la precondicion. Nunca significa "el curso no existe":
// significa que no se sabe, y por eso no se concede ninguna matricula.
public sealed class CourseAvailabilityUnknownException(CourseId courseId)
    : Exception($"No se ha podido verificar la disponibilidad del curso '{courseId.Value}'.")
{
    public CourseId CourseId { get; } = courseId;
}
