using Enrollments.Application.Abstractions;
namespace Enrollments.Application.Enrollments.GetEnrollmentAccess;
public sealed class GetEnrollmentAccessHandler(IEnrollmentRepository enrollments)
{
    public async Task<EnrollmentView?> HandleAsync(
        GetEnrollmentAccessQuery query,
        CancellationToken cancellationToken)
    {
        var enrollment = await enrollments.FindAsync(
            query.StudentId,
            query.CourseId,
            cancellationToken);

        return enrollment is null ? null : EnrollmentView.From(enrollment);
    }
}