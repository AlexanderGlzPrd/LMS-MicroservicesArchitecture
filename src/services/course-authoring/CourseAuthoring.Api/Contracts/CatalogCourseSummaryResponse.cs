using CourseAuthoring.Application.Catalog;

namespace CourseAuthoring.Api.Contracts;

public sealed record CatalogCourseSummaryResponse(
    Guid Id,
    string Title,
    Guid InstructorId,
    int LessonCount,
    DateTimeOffset PublishedAt,
    DateTimeOffset PublishedContentUpdatedAt)
{
    public static CatalogCourseSummaryResponse From(CatalogCourseSummaryView view) => new(
        view.Id,
        view.Title,
        view.InstructorId,
        view.LessonCount,
        view.PublishedAt,
        view.PublishedContentUpdatedAt);
}
