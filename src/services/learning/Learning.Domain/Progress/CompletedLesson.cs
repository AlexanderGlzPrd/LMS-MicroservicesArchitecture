namespace Learning.Domain.Progress;
public sealed class CompletedLesson
{
    private CompletedLesson()
    {
    }

    internal static CompletedLesson Create(LessonId lessonId, DateTimeOffset completedAt) => new()
    {
        LessonId = lessonId,
        CompletedAt = completedAt,
    };

    public LessonId LessonId { get; private set; }

    public DateTimeOffset CompletedAt { get; private set; }
}
