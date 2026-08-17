namespace BffComposition.Api.Contracts;
public sealed record CourseInProgressItemResponse(
    Guid CourseId,
    string? CourseTitle,
    int? LessonCount,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    int CompletedLessonCount,
    decimal? Percentage);
