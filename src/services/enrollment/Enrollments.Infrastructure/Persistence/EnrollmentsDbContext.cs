using Enrollments.Domain.Enrollments;
using Enrollments.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
namespace Enrollments.Infrastructure.Persistence;

public sealed class EnrollmentsDbContext(DbContextOptions<EnrollmentsDbContext> options)
    : DbContext(options)
{
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    internal DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    internal DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    internal DbSet<PurchaseGrant> PurchaseGrants => Set<PurchaseGrant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EnrollmentsDbContext).Assembly);
    }
}