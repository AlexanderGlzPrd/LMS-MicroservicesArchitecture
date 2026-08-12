using CourseAuthoring.Application.Catalog;

namespace CourseAuthoring.Api.Contracts;

public sealed record CatalogCourseResponse(
    Guid Id,
    string Title,
    Guid InstructorId,
    DateTimeOffset PublishedAt,
    DateTimeOffset PublishedContentUpdatedAt,
    IReadOnlyList<LessonResponse> Lessons)
{
    public static CatalogCourseResponse From(CatalogCourseView view) => new(
        view.Id,
        view.Title,
        view.InstructorId,
        view.PublishedAt,
        view.PublishedContentUpdatedAt,
        [.. view.Lessons.Select(LessonResponse.From)]);
}
