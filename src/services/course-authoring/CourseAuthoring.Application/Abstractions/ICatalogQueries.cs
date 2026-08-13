using CourseAuthoring.Application.Catalog;
using CourseAuthoring.Application.Common;
using CourseAuthoring.Domain.Courses;
namespace CourseAuthoring.Application.Abstractions;

public interface ICatalogQueries
{
    Task<PagedResult<CatalogCourseSummaryView>> BrowseAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<CatalogCourseView?> GetPublishedCourseAsync(
        CourseId courseId,
        CancellationToken cancellationToken);

    Task<CourseLessonIdsView?> GetPublishedLessonIdsAsync(
        CourseId courseId,
        CancellationToken cancellationToken);
}
