using CourseAuthoring.Application.Courses;
namespace CourseAuthoring.Api.Contracts;

public sealed record CourseSummaryResponse(
    Guid Id,
    string Title,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? PublishedContentUpdatedAt)
{
    public static CourseSummaryResponse From(CourseSummaryView view) => new(
        view.Id,
        view.Title,
        view.Status,
        view.CreatedAt,
        view.PublishedAt,
        view.PublishedContentUpdatedAt);
}
