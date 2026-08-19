using BuildingBlocks.Observability;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PaidEnrollment.Domain.Purchases;
using PaidEnrollment.Infrastructure.Persistence.Configurations;

namespace PaidEnrollment.Infrastructure.Observability;

internal sealed class PurchaseTraceContextInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Stamp(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var traceContext = OutboxTraceContext.Capture();

        if (traceContext is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries<Purchase>())
        {
            if (entry.State != EntityState.Added)
            {
                continue;
            }

            var property = entry.Property<string?>(PurchaseConfiguration.TraceContextProperty);

            property.CurrentValue ??= traceContext;
        }
    }
}
