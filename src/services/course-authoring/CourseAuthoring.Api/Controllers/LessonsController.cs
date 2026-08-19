using Asp.Versioning;
using CourseAuthoring.Api.Contracts;
using CourseAuthoring.Application.Courses.AddLesson;
using CourseAuthoring.Application.Courses.RemoveLesson;
using CourseAuthoring.Application.Courses.ReorderLessons;
using CourseAuthoring.Application.Courses.UpdateLesson;
using CourseAuthoring.Domain.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseAuthoring.Api.Controllers;

[Authorize(Policy = "Instructor")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/courses/{id:guid}/lessons")]
public sealed class LessonsController(
    AddLessonHandler addLessonHandler,
    UpdateLessonHandler updateLessonHandler,
    RemoveLessonHandler removeLessonHandler,
    ReorderLessonsHandler reorderLessonsHandler) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<LessonResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LessonResponse>> Add(
        Guid id,
        [FromBody] CreateLessonRequest request,
        CancellationToken cancellationToken)
    {
        var view = await addLessonHandler.HandleAsync(
            new AddLessonCommand(new CourseId(id), request.Title, request.Description, request.VideoUrl),
            cancellationToken);

        if (view is null)
        {
            return CourseNotFound(id);
        }

        return StatusCode(StatusCodes.Status201Created, LessonResponse.From(view));
    }

    [HttpPut("{lessonId:guid}")]
    [ProducesResponseType<LessonResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LessonResponse>> Update(
        Guid id,
        Guid lessonId,
        [FromBody] UpdateLessonRequest request,
        CancellationToken cancellationToken)
    {
        var view = await updateLessonHandler.HandleAsync(
            new UpdateLessonCommand(
                new CourseId(id),
                new LessonId(lessonId),
                request.Title,
                request.Description,
                request.VideoUrl),
            cancellationToken);

        if (view is null)
        {
            return CourseNotFound(id);
        }

        return Ok(LessonResponse.From(view));
    }

    [HttpDelete("{lessonId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Remove(
        Guid id,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var removed = await removeLessonHandler.HandleAsync(
            new RemoveLessonCommand(new CourseId(id), new LessonId(lessonId)),
            cancellationToken);

        return removed ? NoContent() : CourseNotFound(id);
    }

    [HttpPut("order")]
    [ProducesResponseType<IReadOnlyList<LessonResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IReadOnlyList<LessonResponse>>> Reorder(
        Guid id,
        [FromBody] ReorderLessonsRequest request,
        CancellationToken cancellationToken)
    {
        var views = await reorderLessonsHandler.HandleAsync(
            new ReorderLessonsCommand(
                new CourseId(id),
                [.. request.LessonIds.Select(lessonId => new LessonId(lessonId))]),
            cancellationToken);

        if (views is null)
        {
            return CourseNotFound(id);
        }

        return Ok(views.Select(LessonResponse.From).ToList());
    }

    private ObjectResult CourseNotFound(Guid id) => Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Curso no encontrado",
        detail: $"No existe ningun curso con identificador {id}.");
}
