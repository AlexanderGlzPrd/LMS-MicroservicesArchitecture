using Asp.Versioning;
using Enrollments.Api.Contracts;
using Enrollments.Application.Enrollments.GetEnrollmentAccess;
using Enrollments.Domain.Enrollments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Enrollments.Api.Controllers;
[Authorize(Policy = "ServiceAccessReader")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/enrollments/access")]
public sealed class EnrollmentAccessController(
    GetEnrollmentAccessHandler getEnrollmentAccessHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<EnrollmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrollmentResponse>> GetAccess(
        [FromQuery] Guid studentId,
        [FromQuery] Guid courseId,
        CancellationToken cancellationToken)
    {
        if (studentId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(studentId), "El identificador del estudiante es obligatorio.");
        }

        if (courseId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(courseId), "El identificador del curso es obligatorio.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var view = await getEnrollmentAccessHandler.HandleAsync(
            new GetEnrollmentAccessQuery(new StudentId(studentId), new CourseId(courseId)),
            cancellationToken);

        if (view is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Matricula no encontrada",
                detail: $"El estudiante {studentId} no esta matriculado en el curso {courseId}.");
        }

        return Ok(EnrollmentResponse.From(view));
    }
}
