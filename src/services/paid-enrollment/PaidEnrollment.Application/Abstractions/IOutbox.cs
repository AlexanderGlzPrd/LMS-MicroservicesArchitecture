using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Application.Abstractions;
public interface IOutbox
{
    void EnqueueAuthorizePayment(Purchase purchase, DateTimeOffset occurredAt);
    void EnqueueCapturePayment(Purchase purchase, DateTimeOffset occurredAt);
    void EnqueueVoidAuthorization(Purchase purchase, DateTimeOffset occurredAt);
    void EnqueueRefundPayment(Purchase purchase, DateTimeOffset occurredAt);
    void EnqueueGetPaymentStatus(Purchase purchase, DateTimeOffset occurredAt);
    void EnqueueGrantEnrollmentForCapturedPayment(Purchase purchase, DateTimeOffset occurredAt);
}