namespace PaidEnrollment.Domain.Purchases;
public enum PurchaseStatus
{
    Started = 1,
    CheckingAccess = 2,
    AuthorizingPayment = 3,
    VerifyingAuthorizationOutcome = 4,
    PaymentAuthorized = 5,
    CapturingPayment = 6,
    VerifyingCaptureOutcome = 7,
    PaymentCaptured = 8,
    GrantingEnrollment = 9,
    VerifyingEnrollmentOutcome = 10,
    EnrollmentGranted = 11,
    Confirmed = 12,
    Rejected = 13,
    Compensating = 14,
    Compensated = 15,
    ManualReview = 16,
    Closed = 17,
}