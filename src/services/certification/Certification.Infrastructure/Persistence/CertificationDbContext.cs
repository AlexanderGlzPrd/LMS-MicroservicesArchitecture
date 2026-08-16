using Microsoft.EntityFrameworkCore;
namespace Certification.Infrastructure.Persistence;

public sealed class CertificationDbContext(DbContextOptions<CertificationDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CertificationDbContext).Assembly);
    }
}
