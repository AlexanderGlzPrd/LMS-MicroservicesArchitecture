namespace Enrollments.Application.Enrollments.GrantEnrollmentForCapturedPayment;
public enum GrantEnrollmentOutcome
{
    AlreadyProcessed = 1,
    Created = 2,
    AlreadyExisted = 3,
    Rejected = 4,
    PurchaseIdConflict = 5,
}