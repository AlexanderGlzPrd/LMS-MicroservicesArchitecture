using PaidEnrollment.Application.Abstractions;
using PaidEnrollment.Application.Abstractions.Exceptions;
using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Application.Purchases.ResolveManualReview;
public sealed class ResolveManualReviewHandler(
    IPurchaseRepository purchases,
    IUnitOfWork unitOfWork,
    ICurrentOperator currentOperator,
    TimeProvider timeProvider)
{
    public async Task<PurchaseView> HandleAsync(
        ResolveManualReviewCommand command,
        CancellationToken cancellationToken)
    {
        var operatorId = currentOperator.OperatorId;

        var purchase = await purchases.FindAsync(command.PurchaseId, cancellationToken)
            ?? throw new PurchaseNotFoundException(command.PurchaseId);

        if (!purchase.IsUnderReview)
        {
            throw new PurchaseNotUnderManualReviewException(purchase.Id, purchase.Status);
        }

        EnsureApplicable(purchase, command.Resolution);

        var now = timeProvider.GetUtcNow();

        Apply(purchase, command.Resolution, now);

        purchases.AddResolution(PurchaseResolution.Record(
            Guid.CreateVersion7(),
            purchase.Id,
            command.Resolution,
            command.Evidence,
            operatorId,
            now));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return PurchaseView.From(purchase);
    }

    private static void Apply(
        Purchase purchase,
        ManualResolution resolution,
        DateTimeOffset now)
    {
        switch (resolution)
        {
            case ManualResolution.ResolveAsConfirmed:
                purchase.ResolveAsConfirmed(now);

                break;

            case ManualResolution.RetryCompensation:
                purchase.RetryCompensation(now);

                break;

            case ManualResolution.ResolveAsCompensated:
                purchase.ResolveAsCompensated(now);

                break;

            default:
                purchase.CloseWithoutAutomaticAction(now);

                break;
        }
    }

    private static void EnsureApplicable(Purchase purchase, ManualResolution resolution)
    {
        var failure = resolution switch
        {
            ManualResolution.ResolveAsConfirmed => ConfirmedPrecondition(purchase),
            ManualResolution.RetryCompensation => purchase.AuthorizedAt is null
                ? "no consta ninguna autorizacion que compensar."
                : null,
            ManualResolution.ResolveAsCompensated =>
                purchase.RefundedAt is null && purchase.VoidedAt is null
                    ? "no consta ni una anulacion ni un reembolso confirmados."
                    : null,

            _ => null,
        };

        if (failure is not null)
        {
            throw new ManualResolutionNotApplicableException(purchase.Id, resolution, failure);
        }
    }

    private static string? ConfirmedPrecondition(Purchase purchase)
    {
        if (purchase.CapturedAt is null)
        {
            return "no consta ninguna captura confirmada.";
        }

        if (purchase.RefundedAt is not null || purchase.VoidedAt is not null)
        {
            return "el pago consta anulado o reembolsado.";
        }

        return purchase.GrantOutcome is GrantOutcome.Created
            or GrantOutcome.AlreadyExistedThisPurchase
            ? null
            : "no consta una concesion de matricula de esta misma compra.";
    }
}
