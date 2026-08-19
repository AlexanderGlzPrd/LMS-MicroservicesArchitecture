namespace PaidEnrollment.Infrastructure.Messaging;
internal sealed class SagaCorrelationMismatchException(string reply, Guid purchaseId)
    : Exception(
        $"La respuesta {reply} no corresponde a la compra '{purchaseId}' que dice identificar.")
{
    public Guid PurchaseId { get; } = purchaseId;
}