using Enrollments.Domain.Enrollments;
using Enrollments.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
namespace Enrollments.Infrastructure.Persistence;

public sealed class EnrollmentsDbContext(DbContextOptions<EnrollmentsDbContext> options)
    : DbContext(options)
{
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    internal DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EnrollmentsDbContext).Assembly);
    }
}
