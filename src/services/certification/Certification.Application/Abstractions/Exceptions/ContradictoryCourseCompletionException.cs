namespace Certification.Application.Abstractions.Exceptions;
public sealed class ContradictoryCourseCompletionException(
    Guid studentId,
    Guid courseId,
    DateTimeOffset registered,
    DateTimeOffset incoming)
    : Exception(
        $"La Finalizacion ({studentId}, {courseId}) ya estaba registrada con " +
        $"CompletedAt '{registered:O}' y el mensaje afirma '{incoming:O}'.")
{
    public Guid StudentId { get; } = studentId;

    public Guid CourseId { get; } = courseId;

    public DateTimeOffset RegisteredCompletedAt { get; } = registered;

    public DateTimeOffset IncomingCompletedAt { get; } = incoming;
}
