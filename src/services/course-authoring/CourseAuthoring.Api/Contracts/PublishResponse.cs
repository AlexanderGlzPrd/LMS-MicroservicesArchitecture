using CourseAuthoring.Application.Courses;

namespace CourseAuthoring.Api.Contracts;

public sealed record PublishResponse(
    string Status,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? PublishedContentUpdatedAt)
{
    public static PublishResponse From(CourseView view) => new(
        view.Status,
        view.PublishedAt,
        view.PublishedContentUpdatedAt);
}
