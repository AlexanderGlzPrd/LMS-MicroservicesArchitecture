using Asp.Versioning;
using Certification.Api.Contracts;
using Certification.Application.Certificates.GetCertificate;
using Certification.Application.Certificates.ListStudentCertificates;
using Certification.Domain.Certificates;
using Microsoft.AspNetCore.Mvc;
namespace Certification.Api.Controllers;
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me/certificates")]
public sealed class MyCertificatesController(
    ListStudentCertificatesHandler listStudentCertificatesHandler,
    GetCertificateHandler getCertificateHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CertificateSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<CertificateSummaryResponse>>> List(
        CancellationToken cancellationToken)
    {
        var views = await listStudentCertificatesHandler.HandleAsync(
            new ListStudentCertificatesQuery(),
            cancellationToken);

        return Ok(views.Select(CertificateSummaryResponse.From).ToArray());
    }

    [HttpGet("{certificateId:guid}")]
    [ProducesResponseType<CertificateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CertificateResponse>> Get(
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

        var view = await getCertificateHandler.HandleAsync(
            new GetCertificateQuery(new CertificateId(certificateId)),
            cancellationToken);

        return view is null ? NotFound() : Ok(CertificateResponse.From(view));
    }
}
