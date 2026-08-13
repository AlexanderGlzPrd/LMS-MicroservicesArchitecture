using Enrollments.Domain.Enrollments;
namespace Enrollments.Application.Abstractions;
public interface IEnrollmentRepository
{
    Task<Enrollment?> FindAsync(
        StudentId studentId,
        CourseId courseId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Enrollment>> ListByStudentAsync(
        StudentId studentId,
        CancellationToken cancellationToken);

    void Add(Enrollment enrollment);
}
