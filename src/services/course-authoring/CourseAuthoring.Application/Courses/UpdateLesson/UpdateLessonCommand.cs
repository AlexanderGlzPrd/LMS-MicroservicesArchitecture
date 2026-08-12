using CourseAuthoring.Domain.Courses;

namespace CourseAuthoring.Application.Courses.UpdateLesson;

public sealed record UpdateLessonCommand(
    CourseId CourseId,
    LessonId LessonId,
    string Title,
    string Description,
    string VideoUrl);
