using CourseAuthoring.Application.Courses;

namespace CourseAuthoring.Api.Contracts;

public sealed record LessonResponse(
    Guid Id,
    string Title,
    string Description,
    string VideoUrl,
    int Position)
{
    public static LessonResponse From(LessonView view) => new(
        view.Id,
        view.Title,
        view.Description,
        view.VideoUrl,
        view.Position);
}
