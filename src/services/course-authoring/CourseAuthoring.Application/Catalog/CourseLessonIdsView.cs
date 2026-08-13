namespace CourseAuthoring.Application.Catalog;

public sealed record CourseLessonIdsView(
    Guid CourseId,
    IReadOnlyList<Guid> LessonIds);
