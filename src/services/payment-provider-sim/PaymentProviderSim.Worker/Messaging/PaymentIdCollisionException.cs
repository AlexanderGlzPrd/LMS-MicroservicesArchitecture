namespace PaymentProviderSim.Worker.Messaging;
internal sealed class PaymentIdCollisionException(Guid paymentId, string discrepancy)
    : Exception(
        $"El PaymentId '{paymentId}' ya existe con un {discrepancy} distinto del recibido.")
{
    public Guid PaymentId { get; } = paymentId;

    public string Discrepancy { get; } = discrepancy;
}