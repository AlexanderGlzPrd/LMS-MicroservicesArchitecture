using CourseAuthoring.Api.Contracts;
using CourseAuthoring.Application.Courses.CreateCourse;
using CourseAuthoring.Application.Courses.GetCourseById;
using CourseAuthoring.Domain.Courses;
using Microsoft.AspNetCore.Mvc;

namespace CourseAuthoring.Api.Controllers;

[ApiController]
[Route("courses")]
public sealed class CoursesController(
    CreateCourseHandler createCourseHandler,
    GetCourseByIdHandler getCourseByIdHandler) : ControllerBase
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

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CourseResponse>(StatusCodes.Status200OK)]
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
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Curso no encontrado",
                detail: $"No existe ningun curso con identificador {id}.");
        }

        return Ok(CourseResponse.From(view));
    }
}
