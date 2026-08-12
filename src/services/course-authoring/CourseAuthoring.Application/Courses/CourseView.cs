using CourseAuthoring.Domain.Courses;
namespace CourseAuthoring.Application.Courses;

public sealed record CourseView(
    Guid Id,
    Guid InstructorId,
    string Title,
    string Status,
    DateTimeOffset CreatedAt)
{
    public static CourseView From(Course course) => new(
        course.Id.Value,
        course.InstructorId.Value,
        course.Title,
        course.Status.ToString(),
        course.CreatedAt);
}
