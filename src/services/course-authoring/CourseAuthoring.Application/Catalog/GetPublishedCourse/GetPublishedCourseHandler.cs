using CourseAuthoring.Application.Abstractions;

namespace CourseAuthoring.Application.Catalog.GetPublishedCourse;

public sealed class GetPublishedCourseHandler(ICatalogQueries catalog)
{
    public Task<CatalogCourseView?> HandleAsync(
        GetPublishedCourseQuery query,
        CancellationToken cancellationToken) =>
        catalog.GetPublishedCourseAsync(query.CourseId, cancellationToken);
}
