using Learning.Domain.Progress;

namespace Learning.Application.Progress;

public sealed record CourseProgressView(
    Guid StudentId,
    Guid CourseId,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<Guid> CompletedLessonIds)
{
    public static CourseProgressView From(CourseProgress progress) => new(
        progress.StudentId.Value,
        progress.CourseId.Value,
        progress.Status.ToString(),
        progress.StartedAt,
        progress.CompletedAt,
        [.. progress.CompletedLessons
            .OrderBy(lesson => lesson.CompletedAt)
            .ThenBy(lesson => lesson.LessonId.Value)
            .Select(lesson => lesson.LessonId.Value)]);
}
