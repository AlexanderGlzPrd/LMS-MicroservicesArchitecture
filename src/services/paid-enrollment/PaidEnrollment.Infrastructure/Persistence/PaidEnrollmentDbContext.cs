using Microsoft.EntityFrameworkCore;
using PaidEnrollment.Domain.Purchases;
using PaidEnrollment.Infrastructure.Messaging;
namespace PaidEnrollment.Infrastructure.Persistence;

public sealed class PaidEnrollmentDbContext(DbContextOptions<PaidEnrollmentDbContext> options)
    : DbContext(options)
{
    public DbSet<Purchase> Purchases => Set<Purchase>();

    internal DbSet<PurchaseResolution> PurchaseResolutions => Set<PurchaseResolution>();

    internal DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    internal DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaidEnrollmentDbContext).Assembly);
    }
}
