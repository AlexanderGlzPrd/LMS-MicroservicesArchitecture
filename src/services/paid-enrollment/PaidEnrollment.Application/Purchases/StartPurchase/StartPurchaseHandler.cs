using PaidEnrollment.Application.Abstractions;
using PaidEnrollment.Application.Abstractions.Exceptions;
using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Application.Purchases.StartPurchase;

public sealed class StartPurchaseHandler(
    IPurchaseRepository purchases,
    IUnitOfWork unitOfWork,
    IPurchaseAmounts amounts,
    ICurrentActor currentActor,
    TimeProvider timeProvider)
{
    public async Task<StartPurchaseResult> HandleAsync(
        StartPurchaseCommand command,
        CancellationToken cancellationToken)
    {
        var studentId = new StudentId(currentActor.StudentId);

        var blocking = await purchases.FindBlockingAsync(
            studentId, command.CourseId, cancellationToken);

        if (blocking is not null)
        {
            return Reuse(blocking);
        }

        var price = amounts.For(command.CourseId)
            ?? throw new PurchaseAmountNotConfiguredException(command.CourseId);

        var purchase = Purchase.Start(
            new PurchaseId(Guid.CreateVersion7()),
            studentId,
            command.CourseId,
            new PaymentId(Guid.CreateVersion7()),
            price,
            timeProvider.GetUtcNow());

        purchases.Add(purchase);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateActivePurchaseException)
        {
            var winner = await RequireBlockingAsync(
                studentId, command.CourseId, cancellationToken);

            return Reuse(winner);
        }

        return new StartPurchaseResult(PurchaseView.From(purchase), Created: true);
    }

    private static StartPurchaseResult Reuse(Purchase blocking) =>
        blocking.Status is PurchaseStatus.Closed
            ? throw new PurchaseClosedForCourseException(blocking.Id, blocking.CourseId)
            : new StartPurchaseResult(PurchaseView.From(blocking), Created: false);

    private async Task<Purchase> RequireBlockingAsync(
        StudentId studentId,
        CourseId courseId,
        CancellationToken cancellationToken)
        => await purchases.FindBlockingAsync(studentId, courseId, cancellationToken)
           ?? throw new InvalidOperationException(
               $"El indice unico rechazo la compra de '{studentId.Value}' en "
               + $"'{courseId.Value}', pero la compra existente no se ha podido releer.");
}
