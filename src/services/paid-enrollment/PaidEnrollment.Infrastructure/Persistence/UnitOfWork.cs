using Microsoft.EntityFrameworkCore;
using Npgsql;
using PaidEnrollment.Application.Abstractions;
using PaidEnrollment.Application.Abstractions.Exceptions;
using PaidEnrollment.Infrastructure.Persistence.Configurations;
namespace PaidEnrollment.Infrastructure.Persistence;
internal sealed class UnitOfWork(PaidEnrollmentDbContext context) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolationOn(
            exception, PurchaseConfiguration.ActiveStudentCourseIndex)
            || IsUniqueViolationOn(exception, PurchaseConfiguration.PaymentIndex))
        {
            context.ChangeTracker.Clear();

            throw new DuplicateActivePurchaseException(exception);
        }
    }

    private static bool IsUniqueViolationOn(DbUpdateException exception, string constraintName) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
        } postgresException
        && postgresException.ConstraintName == constraintName;
}