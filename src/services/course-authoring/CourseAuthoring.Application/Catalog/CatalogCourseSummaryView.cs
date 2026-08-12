namespace CourseAuthoring.Application.Catalog;
public sealed record CatalogCourseSummaryView(
    Guid Id,
    string Title,
    Guid InstructorId,
    int LessonCount,
    DateTimeOffset PublishedAt,
    DateTimeOffset PublishedContentUpdatedAt);