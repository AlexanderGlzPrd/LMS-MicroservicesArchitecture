using System.Text.Json;
using BuildingBlocks.Messaging;
using PaymentProviderSim.Contracts.V1;
using PaymentProviderSim.Worker.Persistence;
namespace PaymentProviderSim.Worker.Messaging;
internal sealed class OutboxWriter(PaymentsDbContext context)
{
    public void Enqueue(PaymentAuthorized contract) => Add(
        contract.PaymentId,
        OutboxContractMapper.PaymentAuthorizedType,
        OutboxContractMapper.PaymentAuthorizedRoutingKey,
        contract,
        contract.OccurredAt);

    public void Enqueue(PaymentDeclined contract) => Add(
        contract.PaymentId,
        OutboxContractMapper.PaymentDeclinedType,
        OutboxContractMapper.PaymentDeclinedRoutingKey,
        contract,
        contract.OccurredAt);

    public void Enqueue(PaymentCaptured contract) => Add(
        contract.PaymentId,
        OutboxContractMapper.PaymentCapturedType,
        OutboxContractMapper.PaymentCapturedRoutingKey,
        contract,
        contract.OccurredAt);

    public void Enqueue(CaptureFailed contract) => Add(
        contract.PaymentId,
        OutboxContractMapper.CaptureFailedType,
        OutboxContractMapper.CaptureFailedRoutingKey,
        contract,
        contract.OccurredAt);

    public void Enqueue(AuthorizationVoided contract) => Add(
        contract.PaymentId,
        OutboxContractMapper.AuthorizationVoidedType,
        OutboxContractMapper.AuthorizationVoidedRoutingKey,
        contract,
        contract.OccurredAt);

    public void Enqueue(PaymentRefunded contract) => Add(
        contract.PaymentId,
        OutboxContractMapper.PaymentRefundedType,
        OutboxContractMapper.PaymentRefundedRoutingKey,
        contract,
        contract.OccurredAt);

    public void Enqueue(RefundFailed contract) => Add(
        contract.PaymentId,
        OutboxContractMapper.RefundFailedType,
        OutboxContractMapper.RefundFailedRoutingKey,
        contract,
        contract.OccurredAt);

    public void Enqueue(PaymentStatusReported contract) => Add(
        contract.PaymentId,
        OutboxContractMapper.PaymentStatusReportedType,
        OutboxContractMapper.PaymentStatusReportedRoutingKey,
        contract,
        contract.OccurredAt);

    private void Add<TContract>(
        Guid paymentId,
        string messageType,
        string routingKey,
        TContract contract,
        DateTimeOffset occurredAt) =>
        context.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            AggregateId = paymentId,
            MessageType = messageType,
            RoutingKey = routingKey,
            Payload = JsonSerializer.Serialize(contract, OutboxSerialization.Options),
            OccurredAt = occurredAt,
        });
}