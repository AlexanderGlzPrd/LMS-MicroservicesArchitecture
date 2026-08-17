namespace BffComposition.Api.Clients.Learning;
internal sealed class CourseProgressResponse
{
    public Guid? CourseId { get; init; }

    public string? Status { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public int? CompletedLessonCount { get; init; }

    public decimal? Percentage { get; init; }
}