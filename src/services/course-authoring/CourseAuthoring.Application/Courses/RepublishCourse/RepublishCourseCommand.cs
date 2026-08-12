using CourseAuthoring.Domain.Courses;

namespace CourseAuthoring.Application.Courses.RepublishCourse;

public sealed record RepublishCourseCommand(CourseId CourseId);
