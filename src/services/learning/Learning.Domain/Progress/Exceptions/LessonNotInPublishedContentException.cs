using Learning.Domain.Abstractions;

namespace Learning.Domain.Progress.Exceptions;

public sealed class LessonNotInPublishedContentException(LessonId lessonId)
    : DomainException($"La leccion {lessonId.Value} no pertenece al contenido publicado vigente del curso.")
{
    public LessonId LessonId { get; } = lessonId;
}
