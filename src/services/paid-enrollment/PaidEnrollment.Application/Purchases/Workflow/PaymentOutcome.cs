namespace PaidEnrollment.Application.Purchases.Workflow;
public enum PaymentOutcome
{
    NotFound = 1,
    Authorized = 2,
    Declined = 3,
    Captured = 4,
    CaptureFailed = 5,
    Voided = 6,
    Refunded = 7,
}
