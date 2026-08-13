using Enrollments.Application.Abstractions;

namespace Enrollments.Application.Enrollments.ListStudentEnrollments;

public sealed class ListStudentEnrollmentsHandler(
    IEnrollmentRepository enrollments,
    ICurrentActor currentActor)
{
    public async Task<IReadOnlyList<EnrollmentView>> HandleAsync(
        ListStudentEnrollmentsQuery query,
        CancellationToken cancellationToken)
    {
        var studentEnrollments = await enrollments.ListByStudentAsync(
            currentActor.StudentId,
            cancellationToken);

        return [.. studentEnrollments.Select(EnrollmentView.From)];
    }
}
