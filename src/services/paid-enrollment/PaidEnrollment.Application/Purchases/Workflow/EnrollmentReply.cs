using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Application.Purchases.Workflow;
public sealed record EnrollmentReply(
    Guid MessageId,
    string MessageType,
    PurchaseId PurchaseId,
    StudentId StudentId,
    CourseId CourseId);
