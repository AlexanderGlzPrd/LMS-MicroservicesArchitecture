using Enrollments.Application.Abstractions;
using Enrollments.Domain.Enrollments;

using Microsoft.EntityFrameworkCore;

namespace Enrollments.Infrastructure.Persistence;

internal sealed class EnrollmentRepository(EnrollmentsDbContext context) : IEnrollmentRepository
{
    public Task<Enrollment?> FindAsync(
        StudentId studentId,
        CourseId courseId,
        CancellationToken cancellationToken) =>
        context.Enrollments
            .FirstOrDefaultAsync(
                enrollment => enrollment.StudentId == studentId && enrollment.CourseId == courseId,
                cancellationToken);

    public async Task<IReadOnlyList<Enrollment>> ListByStudentAsync(
        StudentId studentId,
        CancellationToken cancellationToken) =>
        await context.Enrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.StudentId == studentId)
            .OrderByDescending(enrollment => enrollment.EnrolledAt)
            .ThenBy(enrollment => enrollment.Id)
            .ToListAsync(cancellationToken);

    public void Add(Enrollment enrollment) => context.Enrollments.Add(enrollment);
}
