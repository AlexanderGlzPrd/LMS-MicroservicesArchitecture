using CourseAuthoring.Domain.Courses;

namespace CourseAuthoring.Application.Catalog.GetPublishedLessonIds;

public sealed record GetPublishedLessonIdsQuery(CourseId CourseId);
