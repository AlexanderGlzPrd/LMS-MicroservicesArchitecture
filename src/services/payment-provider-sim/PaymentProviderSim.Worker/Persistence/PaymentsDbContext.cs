using Microsoft.EntityFrameworkCore;
using PaymentProviderSim.Worker.Messaging;
using PaymentProviderSim.Worker.Payments;
namespace PaymentProviderSim.Worker.Persistence;
public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options)
    : DbContext(options)
{
    internal DbSet<Payment> Payments => Set<Payment>();

    internal DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    internal DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentsDbContext).Assembly);
    }
}