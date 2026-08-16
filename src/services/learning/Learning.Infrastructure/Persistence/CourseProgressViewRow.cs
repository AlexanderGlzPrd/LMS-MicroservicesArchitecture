namespace Learning.Infrastructure.Persistence;
internal sealed class CourseProgressViewRow
{
    public required Guid StudentId { get; init; }

    public required Guid CourseId { get; init; }

    public required string Status { get; set; }

    public required DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; set; }

    public required List<Guid> CompletedLessonIds { get; set; }

    public required List<DateTimeOffset> CompletedLessonDates { get; set; }

    public int CompletedLessonCount { get; set; }

    public int? TotalLessonCount { get; set; }
}