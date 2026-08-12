using Asp.Versioning;
using CourseAuthoring.Api.Contracts;
using CourseAuthoring.Application.Courses.CreateCourse;
using CourseAuthoring.Application.Courses.GetCourseById;
using CourseAuthoring.Application.Courses.ListInstructorCourses;
using CourseAuthoring.Application.Courses.PublishCourse;
using CourseAuthoring.Application.Courses.RenameCourse;
using CourseAuthoring.Application.Courses.RepublishCourse;
using CourseAuthoring.Domain.Courses;
using Microsoft.AspNetCore.Mvc;

namespace CourseAuthoring.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/courses")]
public sealed class CoursesController(
    CreateCourseHandler createCourseHandler,
    GetCourseByIdHandler getCourseByIdHandler,
    ListInstructorCoursesHandler listInstructorCoursesHandler,
    RenameCourseHandler renameCourseHandler,
    PublishCourseHandler publishCourseHandler,
    RepublishCourseHandler republishCourseHandler) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CourseResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CourseResponse>> Create(
        [FromBody] CreateCourseRequest request,
        CancellationToken cancellationToken)
    {
        var view = await createCourseHandler.HandleAsync(
            new CreateCourseCommand(request.Title),
            cancellationToken);

        var response = CourseResponse.From(view);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CourseSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<CourseSummaryResponse>>> List(
        CancellationToken cancellationToken)
    {
        var views = await listInstructorCoursesHandler.HandleAsync(
            new ListInstructorCoursesQuery(),
            cancellationToken);

        return Ok(views.Select(CourseSummaryResponse.From).ToList());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CourseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var view = await getCourseByIdHandler.HandleAsync(
            new GetCourseByIdQuery(new CourseId(id)),
            cancellationToken);

        if (view is null)
        {
            return CourseNotFound(id);
        }

        return Ok(CourseResponse.From(view));
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType<CourseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseResponse>> Rename(
        Guid id,
        [FromBody] RenameCourseRequest request,
        CancellationToken cancellationToken)
    {
        var view = await renameCourseHandler.HandleAsync(
            new RenameCourseCommand(new CourseId(id), request.Title),
            cancellationToken);

        if (view is null)
        {
            return CourseNotFound(id);
        }

        return Ok(CourseResponse.From(view));
    }

    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType<PublishResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PublishResponse>> Publish(
        Guid id,
        CancellationToken cancellationToken)
    {
        var view = await publishCourseHandler.HandleAsync(
            new PublishCourseCommand(new CourseId(id)),
            cancellationToken);

        if (view is null)
        {
            return CourseNotFound(id);
        }

        return Ok(PublishResponse.From(view));
    }

    [HttpPost("{id:guid}/republish")]
    [ProducesResponseType<RepublishResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<RepublishResponse>> Republish(
        Guid id,
        CancellationToken cancellationToken)
    {
        var view = await republishCourseHandler.HandleAsync(
            new RepublishCourseCommand(new CourseId(id)),
            cancellationToken);

        if (view is null)
        {
            return CourseNotFound(id);
        }

        return Ok(RepublishResponse.From(view));
    }

    private ObjectResult CourseNotFound(Guid id) => Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Curso no encontrado",
        detail: $"No existe ningun curso con identificador {id}.");
}
