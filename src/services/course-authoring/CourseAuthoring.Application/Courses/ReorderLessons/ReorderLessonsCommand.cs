using CourseAuthoring.Domain.Courses;

namespace CourseAuthoring.Application.Courses.ReorderLessons;

public sealed record ReorderLessonsCommand(CourseId CourseId, IReadOnlyList<LessonId> LessonIds);
