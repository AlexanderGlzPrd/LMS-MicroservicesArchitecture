using CourseAuthoring.Application.Courses;

namespace CourseAuthoring.Api.Contracts;

public sealed record CourseResponse(
    Guid Id,
    Guid InstructorId,
    string Title,
    string Status,
    DateTimeOffset CreatedAt)
{
    public static CourseResponse From(CourseView view) => new(
        view.Id,
        view.InstructorId,
        view.Title,
        view.Status,
        view.CreatedAt);
}
