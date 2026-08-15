using CourseAuthoring.Domain.Courses;
namespace CourseAuthoring.Application.Courses.RemoveLesson;
public sealed record RemoveLessonCommand(CourseId CourseId, LessonId LessonId);