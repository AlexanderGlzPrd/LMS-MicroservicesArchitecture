using CourseAuthoring.Domain.Courses;
namespace CourseAuthoring.Application.Courses;

public sealed record CourseView(
    Guid Id,
    Guid InstructorId,
    string Title,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? PublishedContentUpdatedAt,
    IReadOnlyList<LessonView> Lessons)
{
    public static CourseView From(Course course) => new(
        course.Id.Value,
        course.InstructorId.Value,
        course.Title,
        course.Status.ToString(),
        course.CreatedAt,
        course.PublishedAt,
        course.PublishedContentUpdatedAt,
        [.. course.WorkingLessons.Select(lesson => LessonView.From(lesson))]);
}
