using Asp.Versioning;
using BffComposition.Api.Composition;
using BffComposition.Api.Contracts;
using BffComposition.Api.Errors;
using Microsoft.AspNetCore.Mvc;
namespace BffComposition.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me/courses-in-progress")]
public sealed class MyCoursesInProgressController(CoursesInProgressComposer composer)
    : ControllerBase
{
    private const string StatusQueryKey = "status";
    private const string InProgress = "InProgress";
    private const string Completed = "Completed";

    [HttpGet]
    [ProducesResponseType<CoursesInProgressResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<CoursesInProgressResponse>> Get(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var declaredStatus = Request.Query.TryGetValue(StatusQueryKey, out var values)
            ? values.ToString()
            : status;

        var normalizedStatus = Normalize(declaredStatus);

        var response = await composer.ComposeAsync(normalizedStatus, cancellationToken);

        return Ok(response);
    }

    private static string Normalize(string? status)
    {
        if (status is null)
        {
            return InProgress;
        }

        if (string.Equals(status, InProgress, StringComparison.OrdinalIgnoreCase))
        {
            return InProgress;
        }

        if (string.Equals(status, Completed, StringComparison.OrdinalIgnoreCase))
        {
            return Completed;
        }

        throw new InvalidProgressStatusException();
    }
}
