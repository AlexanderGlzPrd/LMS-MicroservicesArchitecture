using Enrollments.Application.Abstractions;
namespace Enrollments.Application.Enrollments.GetStudentEnrollment;

public sealed class GetStudentEnrollmentHandler(
    IEnrollmentRepository enrollments,
    ICurrentActor currentActor)
{
    public async Task<EnrollmentView?> HandleAsync(
        GetStudentEnrollmentQuery query,
        CancellationToken cancellationToken)
    {
        var enrollment = await enrollments.FindAsync(
            currentActor.StudentId,
            query.CourseId,
            cancellationToken);

        return enrollment is null ? null : EnrollmentView.From(enrollment);
    }
}
