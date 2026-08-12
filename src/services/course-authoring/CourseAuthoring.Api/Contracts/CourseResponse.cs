using CourseAuthoring.Application.Courses;

namespace CourseAuthoring.Api.Contracts;

public sealed record CourseResponse(
    Guid Id,
    Guid InstructorId,
    string Title,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? PublishedContentUpdatedAt,
    IReadOnlyList<LessonResponse> Lessons)
{
    public static CourseResponse From(CourseView view) => new(
        view.Id,
        view.InstructorId,
        view.Title,
        view.Status,
        view.CreatedAt,
        view.PublishedAt,
        view.PublishedContentUpdatedAt,
        [.. view.Lessons.Select(LessonResponse.From)]);
}
