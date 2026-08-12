using System.ComponentModel.DataAnnotations;

using Asp.Versioning;
using CourseAuthoring.Api.Contracts;
using CourseAuthoring.Application.Catalog.BrowseCatalog;
using CourseAuthoring.Application.Catalog.GetPublishedCourse;
using CourseAuthoring.Domain.Courses;
using Microsoft.AspNetCore.Mvc;

namespace CourseAuthoring.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/catalog/courses")]
public sealed class CatalogController(
    BrowseCatalogHandler browseCatalogHandler,
    GetPublishedCourseHandler getPublishedCourseHandler) : ControllerBase
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    [HttpGet]
    [ProducesResponseType<PagedResponse<CatalogCourseSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<CatalogCourseSummaryResponse>>> Browse(
        [FromQuery][Range(1, int.MaxValue)] int page = 1,
        [FromQuery][Range(1, MaxPageSize)] int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await browseCatalogHandler.HandleAsync(
            new BrowseCatalogQuery(page, pageSize),
            cancellationToken);

        return Ok(new PagedResponse<CatalogCourseSummaryResponse>(
            [.. result.Items.Select(CatalogCourseSummaryResponse.From)],
            result.Page,
            result.PageSize,
            result.TotalCount));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CatalogCourseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CatalogCourseResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var view = await getPublishedCourseHandler.HandleAsync(
            new GetPublishedCourseQuery(new CourseId(id)),
            cancellationToken);

        if (view is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Curso no encontrado en el catalogo",
                detail: $"No hay ningun curso publicado con identificador {id}.");
        }

        return Ok(CatalogCourseResponse.From(view));
    }
}
