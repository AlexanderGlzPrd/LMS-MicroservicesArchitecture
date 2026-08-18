using Enrollments.Application.Abstractions;
using Enrollments.Domain.Enrollments;
namespace Enrollments.Application.Enrollments.GrantEnrollmentForCapturedPayment;

public sealed record GrantEnrollmentForCapturedPaymentCommand(
    Guid MessageId,
    string MessageType,
    PurchaseId PurchaseId,
    StudentId StudentId,
    CourseId CourseId,
    DateTimeOffset OccurredAt);
