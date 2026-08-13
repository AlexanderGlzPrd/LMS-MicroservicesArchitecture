using CourseAuthoring.Application.Catalog;

namespace CourseAuthoring.Api.Contracts;

public sealed record CatalogCourseLessonIdsResponse(
    Guid CourseId,
    IReadOnlyList<Guid> LessonIds)
{
    public static CatalogCourseLessonIdsResponse From(CourseLessonIdsView view) => new(
        view.CourseId,
        view.LessonIds);
}
