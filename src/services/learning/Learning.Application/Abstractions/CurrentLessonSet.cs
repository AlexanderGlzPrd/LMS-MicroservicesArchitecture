using Learning.Domain.Progress;

namespace Learning.Application.Abstractions;

public sealed class CurrentLessonSet
{
    private static readonly IReadOnlySet<LessonId> Empty = new HashSet<LessonId>();

    private CurrentLessonSet(CurrentLessonSetStatus status, IReadOnlySet<LessonId> lessonIds)
    {
        Status = status;
        LessonIds = lessonIds;
    }
    public static CurrentLessonSet Available(IReadOnlySet<LessonId> lessonIds)
    {
        ArgumentNullException.ThrowIfNull(lessonIds);

        if (lessonIds.Count == 0)
        {
            throw new ArgumentException(
                "Un conjunto de lecciones disponible no puede estar vacio: un curso publicado tiene al menos una leccion.",
                nameof(lessonIds));
        }

        return new CurrentLessonSet(CurrentLessonSetStatus.Available, lessonIds);
    }

    public static readonly CurrentLessonSet NotAvailable =
        new(CurrentLessonSetStatus.NotAvailable, Empty);

    public static readonly CurrentLessonSet Unknown =
        new(CurrentLessonSetStatus.Unknown, Empty);

    public CurrentLessonSetStatus Status { get; }

    public IReadOnlySet<LessonId> LessonIds { get; }
}
