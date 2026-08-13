using Asp.Versioning;
using Enrollments.Api.Contracts;
using Enrollments.Application.Enrollments.GetStudentEnrollment;
using Enrollments.Application.Enrollments.ListStudentEnrollments;
using Enrollments.Domain.Enrollments;
using Microsoft.AspNetCore.Mvc;
namespace Enrollments.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me/enrollments")]
public sealed class MyEnrollmentsController(
    ListStudentEnrollmentsHandler listStudentEnrollmentsHandler,
    GetStudentEnrollmentHandler getStudentEnrollmentHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EnrollmentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<EnrollmentResponse>>> List(
        CancellationToken cancellationToken)
    {
        var views = await listStudentEnrollmentsHandler.HandleAsync(
            new ListStudentEnrollmentsQuery(),
            cancellationToken);

        return Ok(views.Select(EnrollmentResponse.From).ToList());
    }

    [HttpGet("{courseId:guid}")]
    [ProducesResponseType<EnrollmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrollmentResponse>> GetByCourse(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var view = await getStudentEnrollmentHandler.HandleAsync(
            new GetStudentEnrollmentQuery(new CourseId(courseId)),
            cancellationToken);

        if (view is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Matricula no encontrada",
                detail: $"El estudiante no esta matriculado en el curso {courseId}.");
        }

        return Ok(EnrollmentResponse.From(view));
    }
}
