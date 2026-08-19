using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PaidEnrollment.Api.Contracts;
using PaidEnrollment.Application.Purchases.GetPurchase;
using PaidEnrollment.Application.Purchases.ResolveManualReview;
using PaidEnrollment.Application.Purchases.StartPurchase;
using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Api.Controllers;
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/purchases")]
public sealed class PurchasesController(
    StartPurchaseHandler startPurchaseHandler,
    GetPurchaseHandler getPurchaseHandler,
    ResolveManualReviewHandler resolveManualReviewHandler) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<PurchaseResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<PurchaseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PurchaseResponse>> Start(
        [FromBody] StartPurchaseRequest request,
        CancellationToken cancellationToken)
    {
        if (request.CourseId == Guid.Empty)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Curso no valido",
                detail: "El campo courseId es obligatorio y no puede estar vacio.");
        }

        var result = await startPurchaseHandler.HandleAsync(
            new StartPurchaseCommand(new CourseId(request.CourseId)),
            cancellationToken);

        var response = PurchaseResponse.From(result.Purchase);

        if (!result.Created)
        {
            return Ok(response);
        }

        return AcceptedAtAction(
            nameof(GetById),
            new { purchaseId = response.PurchaseId },
            response);
    }

    [HttpGet("{purchaseId:guid}")]
    [ProducesResponseType<PurchaseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseResponse>> GetById(
        Guid purchaseId,
        CancellationToken cancellationToken)
    {
        var view = await getPurchaseHandler.HandleAsync(
            new GetPurchaseQuery(new PurchaseId(purchaseId)),
            cancellationToken);

        return Ok(PurchaseResponse.Detailed(view));
    }

    [HttpPost("{purchaseId:guid}/resolutions")]
    [ProducesResponseType<PurchaseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PurchaseResponse>> Resolve(
        Guid purchaseId,
        [FromBody] ResolvePurchaseRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ManualResolution>(request.Resolution, out var resolution))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Resolucion desconocida",
                detail: "resolution debe ser ResolveAsConfirmed, RetryCompensation, "
                    + "ResolveAsCompensated o CloseWithoutAutomaticAction.");
        }

        var view = await resolveManualReviewHandler.HandleAsync(
            new ResolveManualReviewCommand(
                new PurchaseId(purchaseId), resolution, request.Evidence.Trim()),
            cancellationToken);

        return Ok(PurchaseResponse.Detailed(view));
    }
}