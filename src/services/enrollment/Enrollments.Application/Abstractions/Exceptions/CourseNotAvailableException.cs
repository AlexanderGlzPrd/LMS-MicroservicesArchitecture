using Enrollments.Domain.Enrollments;

namespace Enrollments.Application.Abstractions.Exceptions;

// El curso no es matriculable: Course Authoring no lo publica hoy. La peticion esta
// bien escrita y solo es inejecutable contra el estado actual.
public sealed class CourseNotAvailableException(CourseId courseId)
    : Exception($"El curso '{courseId.Value}' no esta disponible para matricula.")
{
    public CourseId CourseId { get; } = courseId;
}
