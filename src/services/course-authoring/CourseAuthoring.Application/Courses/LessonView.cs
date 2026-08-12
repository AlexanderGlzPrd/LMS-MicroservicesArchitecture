using CourseAuthoring.Domain.Courses;
namespace CourseAuthoring.Application.Courses;

public sealed record LessonView(
    Guid Id,
    string Title,
    string Description,
    string VideoUrl,
    int Position)
{
    public static LessonView From(Lesson lesson) => new(
        lesson.Id.Value,
        lesson.Title,
        lesson.Description,
        lesson.VideoUrl,
        lesson.Position);
}
