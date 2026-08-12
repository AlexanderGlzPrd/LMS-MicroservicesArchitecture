using CourseAuthoring.Application.Courses.RepublishCourse;

namespace CourseAuthoring.Api.Contracts;

public sealed record RepublishResponse(bool Changed, DateTimeOffset? PublishedContentUpdatedAt)
{
    public static RepublishResponse From(RepublishResultView view) => new(
        view.Changed,
        view.PublishedContentUpdatedAt);
}
