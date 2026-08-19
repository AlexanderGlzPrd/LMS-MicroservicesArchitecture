using Asp.Versioning;
using Certification.Api.Contracts;
using Certification.Application.Certificates.VerifyCertificate;
using Certification.Domain.Certificates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Certification.Api.Controllers;
[AllowAnonymous]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/certificates")]
public sealed class CertificateVerificationController(
    VerifyCertificateHandler verifyCertificateHandler) : ControllerBase
{
    [HttpGet("{certificateId:guid}")]
    [ProducesResponseType<CertificateVerificationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CertificateVerificationResponse>> Verify(
        Guid certificateId,
        CancellationToken cancellationToken)
    {
        if (certificateId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(certificateId),
                "El identificador del certificado no puede ser un GUID nulo.");

            return ValidationProblem(ModelState);
        }

        var view = await verifyCertificateHandler.HandleAsync(
            new VerifyCertificateQuery(new CertificateId(certificateId)),
            cancellationToken);

        return view is null
            ? NotFound()
            : Ok(CertificateVerificationResponse.From(view));
    }
}
