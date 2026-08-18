namespace PaidEnrollment.Domain.Purchases;
public enum PurchaseReason
{
    AlreadyEnrolled = 1,
    PreCheckUnavailable = 2,
    PaymentDeclined = 3,
    AuthorizationNotFound = 4,

    AuthorizationVoided = 5,
    PaymentRefunded = 6,
    ManuallyResolved = 7,

    AuthorizationOutcomeUnknown = 8,
    CaptureOutcomeUnknown = 9,
    CaptureOutcomeInconsistent = 10,
    EnrollmentOutcomeUnknown = 11,
    AccessFromAnotherOrigin = 12,
    RefundFailed = 13,
    CompensationOutcomeUnknown = 14,

    ClosedByOperator = 15,
}