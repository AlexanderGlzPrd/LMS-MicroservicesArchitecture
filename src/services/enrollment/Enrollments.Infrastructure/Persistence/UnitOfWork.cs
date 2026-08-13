using Enrollments.Application.Abstractions;
using Enrollments.Application.Abstractions.Exceptions;
using Enrollments.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace Enrollments.Infrastructure.Persistence;

internal sealed class UnitOfWork(EnrollmentsDbContext context) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateEnrollment(exception))
        {
            throw new DuplicateEnrollmentException(exception);
        }
    }

    private static bool IsDuplicateEnrollment(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: EnrollmentConfiguration.UniqueStudentCourseIndex,
        };
}
