using Asp.Versioning;
using Learning.Api.Contracts;
using Learning.Application.Progress.GetCourseProgress;
using Learning.Application.Progress.ListStudentProgress;
using Learning.Domain.Progress;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Learning.Api.Controllers;

[Authorize(Policy = "Student")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me/course-progress")]
public sealed class MyCourseProgressController(
    ListStudentProgressHandler listStudentProgressHandler,
    GetCourseProgressHandler getCourseProgressHandler) : ControllerBase
{
    private const string StatusQueryKey = "status";

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CourseProgressResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<CourseProgressResponse>>> List(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var declaredStatus = Request.Query.TryGetValue(StatusQueryKey, out var values)
            ? values.ToString()
            : status;

        if (!TryParseStatus(declaredStatus, out var parsedStatus))
        {
            ModelState.AddModelError(
                StatusQueryKey,
                "El filtro 'status' solo admite los valores 'InProgress' y 'Completed'.");

            return ValidationProblem(ModelState);
        }

        var views = await listStudentProgressHandler.HandleAsync(
            new ListStudentProgressQuery(parsedStatus),
            cancellationToken);

        return Ok(views.Select(CourseProgressResponse.From).ToList());
    }

    [HttpGet("{courseId:guid}")]
    [ProducesResponseType<CourseProgressResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseProgressResponse>> GetByCourse(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var view = await getCourseProgressHandler.HandleAsync(
            new GetCourseProgressQuery(new CourseId(courseId)),
            cancellationToken);

        if (view is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Progreso no encontrado",
                detail: $"El estudiante no tiene progreso en el curso {courseId}.");
        }

        return Ok(CourseProgressResponse.From(view));
    }

    private static bool TryParseStatus(string? status, out CourseProgressStatus? parsed)
    {
        parsed = null;

        if (status is null)
        {
            return true;
        }

        if (string.Equals(status, nameof(CourseProgressStatus.InProgress), StringComparison.OrdinalIgnoreCase))
        {
            parsed = CourseProgressStatus.InProgress;

            return true;
        }

        if (string.Equals(status, nameof(CourseProgressStatus.Completed), StringComparison.OrdinalIgnoreCase))
        {
            parsed = CourseProgressStatus.Completed;

            return true;
        }

        return false;
    }
}
