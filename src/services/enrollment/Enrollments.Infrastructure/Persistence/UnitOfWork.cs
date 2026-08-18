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
        catch (DbUpdateException exception) when (IsUniqueViolationOn(
            exception, EnrollmentConfiguration.UniqueStudentCourseIndex))
        {
            context.ChangeTracker.Clear();
            throw new DuplicateEnrollmentException(exception);
        }
        catch (DbUpdateException exception) when (IsUniqueViolationOn(
            exception, OutboxMessageConfiguration.UniqueAggregateMessageTypeIndex))
        {
            context.ChangeTracker.Clear();
            throw new DuplicateOutboxMessageException(exception);
        }
        catch (DbUpdateException exception) when (IsUniqueViolationOn(
            exception, PurchaseGrantConfiguration.PrimaryKeyName))
        {
            context.ChangeTracker.Clear();
            throw new DuplicatePurchaseGrantException(exception);
        }
    }

    private static bool IsUniqueViolationOn(DbUpdateException exception, string constraintName) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
        } postgresException
        && postgresException.ConstraintName == constraintName;
}
