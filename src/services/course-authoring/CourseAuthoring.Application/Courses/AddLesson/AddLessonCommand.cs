using CourseAuthoring.Domain.Courses;

namespace CourseAuthoring.Application.Courses.AddLesson;

public sealed record AddLessonCommand(
    CourseId CourseId,
    string Title,
    string Description,
    string VideoUrl);
