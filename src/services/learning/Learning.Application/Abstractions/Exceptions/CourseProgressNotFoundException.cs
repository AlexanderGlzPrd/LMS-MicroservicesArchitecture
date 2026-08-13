using Learning.Domain.Progress;
namespace Learning.Application.Abstractions.Exceptions;

public sealed class CourseProgressNotFoundException(StudentId studentId, CourseId courseId)
    : Exception($"El estudiante '{studentId.Value}' no tiene progreso en el curso '{courseId.Value}'.")
{
    public StudentId StudentId { get; } = studentId;

    public CourseId CourseId { get; } = courseId;
}
