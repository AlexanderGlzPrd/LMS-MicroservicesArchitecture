using Learning.Domain.Progress;

namespace Learning.Application.Progress;

public sealed record CourseProgressView(
    Guid StudentId,
    Guid CourseId,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<Guid> CompletedLessonIds,
    int CompletedLessonCount,
    int? TotalLessonCount)
{
    private static readonly string CompletedStatus = CourseProgressStatus.Completed.ToString();

    public static CourseProgressView From(
        CourseProgress progress,
        int observedTotalLessonCount)
    {
        var completedLessonIds = progress.CompletedLessons
            .OrderBy(lesson => lesson.CompletedAt)
            .ThenBy(lesson => lesson.LessonId.Value)
            .Select(lesson => lesson.LessonId.Value)
            .ToArray();

        return new CourseProgressView(
            progress.StudentId.Value,
            progress.CourseId.Value,
            progress.Status.ToString(),
            progress.StartedAt,
            progress.CompletedAt,
            completedLessonIds,
            completedLessonIds.Length,
            observedTotalLessonCount);
    }

    public decimal? Percentage =>
        Status == CompletedStatus
            ? 100m
            : TotalLessonCount is > 0
                ? Math.Round(
                    CompletedLessonCount * 100m / TotalLessonCount.Value,
                    2,
                    MidpointRounding.AwayFromZero)
                : null;
}
