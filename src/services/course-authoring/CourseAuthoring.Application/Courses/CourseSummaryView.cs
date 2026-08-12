using CourseAuthoring.Domain.Courses;
namespace CourseAuthoring.Application.Courses;

public sealed record CourseSummaryView(
    Guid Id,
    string Title,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? PublishedContentUpdatedAt)
{
    public static CourseSummaryView From(Course course) => new(
        course.Id.Value,
        course.Title,
        course.Status.ToString(),
        course.CreatedAt,
        course.PublishedAt,
        course.PublishedContentUpdatedAt);
}
