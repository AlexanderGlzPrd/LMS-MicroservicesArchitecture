using CourseAuthoring.Domain.Courses;

namespace CourseAuthoring.Application.Courses.RenameCourse;

public sealed record RenameCourseCommand(CourseId CourseId, string Title);
