using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Application.Abstractions.Exceptions;
public sealed class PurchaseClosedForCourseException(PurchaseId purchaseId, CourseId courseId)
    : Exception(
        $"La compra '{purchaseId.Value}' del curso '{courseId.Value}' se cerro sin resolver y "
        + "bloquea el inicio de otra.")
{
    public PurchaseId PurchaseId { get; } = purchaseId;

    public CourseId CourseId { get; } = courseId;
}