using PaidEnrollment.Application.Abstractions;
using PaidEnrollment.Application.Abstractions.Exceptions;
using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Application.Purchases.GetPurchase;
public sealed class GetPurchaseHandler(
    IPurchaseRepository purchases,
    ICurrentActor currentActor)
{
    public async Task<PurchaseView> HandleAsync(
        GetPurchaseQuery query,
        CancellationToken cancellationToken)
    {
        var studentId = new StudentId(currentActor.StudentId);

        var purchase = await purchases.FindAsync(query.PurchaseId, cancellationToken);

        return purchase is null || purchase.StudentId != studentId
            ? throw new PurchaseNotFoundException(query.PurchaseId)
            : PurchaseView.From(purchase);
    }
}
