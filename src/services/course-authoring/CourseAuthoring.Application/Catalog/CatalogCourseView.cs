using CourseAuthoring.Application.Courses;

namespace CourseAuthoring.Application.Catalog;

public sealed record CatalogCourseView(
    Guid Id,
    string Title,
    Guid InstructorId,
    DateTimeOffset PublishedAt,
    DateTimeOffset PublishedContentUpdatedAt,
    IReadOnlyList<LessonView> Lessons);
