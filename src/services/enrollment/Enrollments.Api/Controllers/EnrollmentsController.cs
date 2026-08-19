using Asp.Versioning;
using Enrollments.Api.Contracts;
using Enrollments.Application.Enrollments.EnrollStudent;
using Enrollments.Domain.Enrollments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Enrollments.Api.Controllers;

[Authorize(Policy = "Student")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/enrollments")]
public sealed class EnrollmentsController(EnrollStudentHandler enrollStudentHandler) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<EnrollmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<EnrollmentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<EnrollmentResponse>> Enroll(
        [FromBody] CreateEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.CourseId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(CreateEnrollmentRequest.CourseId),
                "El identificador del curso no puede ser un GUID nulo.");

            return ValidationProblem(ModelState);
        }

        var result = await enrollStudentHandler.HandleAsync(
            new EnrollStudentCommand(new CourseId(request.CourseId!.Value)),
            cancellationToken);

        var response = EnrollmentResponse.From(result.Enrollment);

        return result.Created
            ? Created($"/api/v1/me/enrollments/{response.CourseId}", response)
            : Ok(response);
    }
}
