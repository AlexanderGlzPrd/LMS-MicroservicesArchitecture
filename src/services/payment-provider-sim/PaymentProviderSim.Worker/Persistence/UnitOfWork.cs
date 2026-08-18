using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace PaymentProviderSim.Worker.Persistence;
internal sealed class UnitOfWork(PaymentsDbContext context)
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            context.ChangeTracker.Clear();

            throw new DuplicatePaymentCommandException(exception);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
        };
}