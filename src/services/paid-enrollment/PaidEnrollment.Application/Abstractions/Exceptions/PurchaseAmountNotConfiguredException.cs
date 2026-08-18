using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Application.Abstractions.Exceptions;
public sealed class PurchaseAmountNotConfiguredException(CourseId courseId)
    : Exception($"El curso '{courseId.Value}' no tiene un importe configurado.")
{
    public CourseId CourseId { get; } = courseId;
}