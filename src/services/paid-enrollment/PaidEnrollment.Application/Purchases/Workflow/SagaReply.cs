using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Application.Purchases.Workflow;
public sealed record SagaReply(
    Guid MessageId,
    string MessageType,
    PurchaseId PurchaseId,
    PaymentId PaymentId);
