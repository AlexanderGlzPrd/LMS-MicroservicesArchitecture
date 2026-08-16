namespace Learning.Infrastructure.Projection;
internal sealed class MissingCourseProgressViewException(
    Guid progressEventId, Guid studentId, Guid courseId)
    : Exception(
        $"El evento '{progressEventId}' no encuentra la fila de proyeccion " +
        $"del progreso ({studentId}, {courseId}).")
{
    public Guid ProgressEventId { get; } = progressEventId;

    public Guid StudentId { get; } = studentId;

    public Guid CourseId { get; } = courseId;
}
