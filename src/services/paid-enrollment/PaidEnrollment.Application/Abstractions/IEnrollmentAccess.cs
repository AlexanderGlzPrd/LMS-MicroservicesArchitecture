using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Application.Abstractions;
public interface IEnrollmentAccess
{
    Task<EnrollmentAccess> CheckAsync(
        StudentId studentId,
        CourseId courseId,
        CancellationToken cancellationToken);
}
