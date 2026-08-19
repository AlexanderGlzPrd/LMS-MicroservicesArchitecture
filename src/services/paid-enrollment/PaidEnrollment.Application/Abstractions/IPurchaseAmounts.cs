using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Application.Abstractions;
public interface IPurchaseAmounts
{
    Money? For(CourseId courseId);
}