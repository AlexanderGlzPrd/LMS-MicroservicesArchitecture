using Asp.Versioning;
using Learning.Api.Contracts;
using Learning.Application.Progress.ConfirmCompletion;
using Learning.Application.Progress.MarkLessonCompleted;
using Learning.Domain.Progress;

using Microsoft.AspNetCore.Mvc;

namespace Learning.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me/course-progress")]
public sealed class CourseProgressCommandsController(
    MarkLessonCompletedHandler markLessonCompletedHandler,
    ConfirmCompletionHandler confirmCompletionHandler) : ControllerBase
{
    [HttpPost("{courseId:guid}/completed-lessons")]
    [ProducesResponseType<CourseProgressResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<CourseProgressResponse>> MarkLessonCompleted(
        Guid courseId,
        [FromBody] MarkLessonCompletedRequest request,
        CancellationToken cancellationToken)
    {
        if (courseId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(courseId),
                "El identificador del curso no puede ser un GUID nulo.");

            return ValidationProblem(ModelState);
        }

        if (request.LessonId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(MarkLessonCompletedRequest.LessonId),
                "El identificador de la leccion no puede ser un GUID nulo.");

            return ValidationProblem(ModelState);
        }

        var view = await markLessonCompletedHandler.HandleAsync(
            new MarkLessonCompletedCommand(new CourseId(courseId), new LessonId(request.LessonId!.Value)),
            cancellationToken);

        return Ok(CourseProgressResponse.From(view));
    }

    [HttpPost("{courseId:guid}/completion")]
    [ProducesResponseType<CourseProgressResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<CourseProgressResponse>> ConfirmCompletion(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        if (courseId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(courseId),
                "El identificador del curso no puede ser un GUID nulo.");

            return ValidationProblem(ModelState);
        }

        var view = await confirmCompletionHandler.HandleAsync(
            new ConfirmCompletionCommand(new CourseId(courseId)),
            cancellationToken);

        return Ok(CourseProgressResponse.From(view));
    }
}
