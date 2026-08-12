using CourseAuthoring.Application.Abstractions;
using CourseAuthoring.Application.Common;

namespace CourseAuthoring.Application.Catalog.BrowseCatalog;

public sealed class BrowseCatalogHandler(ICatalogQueries catalog)
{
    public Task<PagedResult<CatalogCourseSummaryView>> HandleAsync(
        BrowseCatalogQuery query,
        CancellationToken cancellationToken) =>
        catalog.BrowseAsync(query.Page, query.PageSize, cancellationToken);
}
