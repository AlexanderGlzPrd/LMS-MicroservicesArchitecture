using Certification.Application.Abstractions;
using Certification.Application.Abstractions.Exceptions;
using Certification.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace Certification.Infrastructure.Persistence;
internal sealed class UnitOfWork(CertificationDbContext context) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolationOn(
            exception, CertificateConfiguration.UniqueStudentCourseIndex))
        {
            context.ChangeTracker.Clear();
            throw new DuplicateCertificateException(exception);
        }
    }

    private static bool IsUniqueViolationOn(DbUpdateException exception, string constraintName) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
        } postgresException
        && postgresException.ConstraintName == constraintName;
}
