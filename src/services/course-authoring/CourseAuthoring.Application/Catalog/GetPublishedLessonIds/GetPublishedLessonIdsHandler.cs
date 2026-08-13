using CourseAuthoring.Application.Abstractions;

namespace CourseAuthoring.Application.Catalog.GetPublishedLessonIds;

public sealed class GetPublishedLessonIdsHandler(ICatalogQueries catalog)
{
    public Task<CourseLessonIdsView?> HandleAsync(
        GetPublishedLessonIdsQuery query,
        CancellationToken cancellationToken) =>
        catalog.GetPublishedLessonIdsAsync(query.CourseId, cancellationToken);
}
