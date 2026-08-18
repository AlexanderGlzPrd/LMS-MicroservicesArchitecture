using System.Text.Json;
using BuildingBlocks.Messaging;
using PaidEnrollment.Application.Abstractions;
using PaidEnrollment.Contracts.V1;
using PaidEnrollment.Domain.Purchases;
using PaidEnrollment.Infrastructure.Persistence;
namespace PaidEnrollment.Infrastructure.Messaging;
internal sealed class OutboxWriter(PaidEnrollmentDbContext context) : IOutbox
{
    public void EnqueueAuthorizePayment(Purchase purchase, DateTimeOffset occurredAt) => Add(
        purchase.Id,
        OutboxContractMapper.AuthorizePaymentType,
        OutboxContractMapper.AuthorizePaymentRoutingKey,
        new AuthorizePayment
        {
            PurchaseId = purchase.Id.Value,
            PaymentId = purchase.PaymentId.Value,
            Amount = purchase.Price.Amount,
            Currency = purchase.Price.Currency,
            OccurredAt = occurredAt,
        },
        occurredAt);

    public void EnqueueCapturePayment(Purchase purchase, DateTimeOffset occurredAt) => Add(
        purchase.Id,
        OutboxContractMapper.CapturePaymentType,
        OutboxContractMapper.CapturePaymentRoutingKey,
        new CapturePayment
        {
            PurchaseId = purchase.Id.Value,
            PaymentId = purchase.PaymentId.Value,
            OccurredAt = occurredAt,
        },
        occurredAt);

    public void EnqueueVoidAuthorization(Purchase purchase, DateTimeOffset occurredAt) => Add(
        purchase.Id,
        OutboxContractMapper.VoidAuthorizationType,
        OutboxContractMapper.VoidAuthorizationRoutingKey,
        new VoidAuthorization
        {
            PurchaseId = purchase.Id.Value,
            PaymentId = purchase.PaymentId.Value,
            OccurredAt = occurredAt,
        },
        occurredAt);

    public void EnqueueRefundPayment(Purchase purchase, DateTimeOffset occurredAt) => Add(
        purchase.Id,
        OutboxContractMapper.RefundPaymentType,
        OutboxContractMapper.RefundPaymentRoutingKey,
        new RefundPayment
        {
            PurchaseId = purchase.Id.Value,
            PaymentId = purchase.PaymentId.Value,
            OccurredAt = occurredAt,
        },
        occurredAt);

    public void EnqueueGetPaymentStatus(Purchase purchase, DateTimeOffset occurredAt) => Add(
        purchase.Id,
        OutboxContractMapper.GetPaymentStatusType,
        OutboxContractMapper.GetPaymentStatusRoutingKey,
        new GetPaymentStatus
        {
            PurchaseId = purchase.Id.Value,
            PaymentId = purchase.PaymentId.Value,
            OccurredAt = occurredAt,
        },
        occurredAt);

    public void EnqueueGrantEnrollmentForCapturedPayment(
        Purchase purchase,
        DateTimeOffset occurredAt) => Add(
        purchase.Id,
        OutboxContractMapper.GrantEnrollmentForCapturedPaymentType,
        OutboxContractMapper.GrantEnrollmentForCapturedPaymentRoutingKey,
        new GrantEnrollmentForCapturedPayment
        {
            PurchaseId = purchase.Id.Value,
            StudentId = purchase.StudentId.Value,
            CourseId = purchase.CourseId.Value,
            OccurredAt = occurredAt,
        },
        occurredAt);

    private void Add<TContract>(
        PurchaseId purchaseId,
        string messageType,
        string routingKey,
        TContract contract,
        DateTimeOffset occurredAt) =>
        context.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            AggregateId = purchaseId.Value,
            MessageType = messageType,
            RoutingKey = routingKey,
            Payload = JsonSerializer.Serialize(contract, OutboxSerialization.Options),
            OccurredAt = occurredAt,
        });
}