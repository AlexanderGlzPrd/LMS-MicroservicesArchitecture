using Enrollments.Application.Abstractions;
using Enrollments.Domain.Enrollments;
namespace Enrollments.Application.Tests.Fakes;

internal sealed class InMemoryEnrollmentRepository : IEnrollmentRepository
{
    private readonly List<Enrollment> stored = [];
    private readonly List<Enrollment> pending = [];

    public int StoredCount => stored.Count;

    public void Seed(Enrollment enrollment) => stored.Add(enrollment);

    public void Commit()
    {
        stored.AddRange(pending);
        pending.Clear();
    }

    public void DiscardPending() => pending.Clear();

    public Task<Enrollment?> FindAsync(
        StudentId studentId,
        CourseId courseId,
        CancellationToken cancellationToken) =>
        Task.FromResult(stored.SingleOrDefault(
            enrollment => enrollment.StudentId == studentId && enrollment.CourseId == courseId));

    public Task<IReadOnlyList<Enrollment>> ListByStudentAsync(
        StudentId studentId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Enrollment>>(
        [
            .. stored
                .Where(enrollment => enrollment.StudentId == studentId)
                .OrderByDescending(enrollment => enrollment.EnrolledAt)
                .ThenBy(enrollment => enrollment.Id.Value)
        ]);

    public void Add(Enrollment enrollment) => pending.Add(enrollment);
}
