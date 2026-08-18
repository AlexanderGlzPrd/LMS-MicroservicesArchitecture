using Microsoft.EntityFrameworkCore;
namespace PaidEnrollment.Infrastructure.Persistence;
public sealed class PaidEnrollmentDbContext(DbContextOptions<PaidEnrollmentDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaidEnrollmentDbContext).Assembly);
    }
}