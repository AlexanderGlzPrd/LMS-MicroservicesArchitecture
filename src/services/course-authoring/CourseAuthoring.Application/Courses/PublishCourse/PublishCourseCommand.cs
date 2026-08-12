using CourseAuthoring.Domain.Courses;

namespace CourseAuthoring.Application.Courses.PublishCourse;

public sealed record PublishCourseCommand(CourseId CourseId);
