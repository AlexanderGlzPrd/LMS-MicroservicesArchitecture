using Learning.Application.Progress;
namespace Learning.Api.Contracts;

public sealed record CourseProgressResponse(
    Guid StudentId,
    Guid CourseId,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<Guid> CompletedLessonIds)
{
    public static CourseProgressResponse From(CourseProgressView view) => new(
        view.StudentId,
        view.CourseId,
        view.Status,
        view.StartedAt,
        view.CompletedAt,
        view.CompletedLessonIds);
}
