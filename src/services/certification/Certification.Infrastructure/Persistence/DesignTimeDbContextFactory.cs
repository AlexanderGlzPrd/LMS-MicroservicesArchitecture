using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace Certification.Infrastructure.Persistence;

internal sealed class DesignTimeDbContextFactory
    : IDesignTimeDbContextFactory<CertificationDbContext>
{
    private const string LocalDevelopmentConnection =
        "Host=localhost;Port=5432;Database=certification;" +
        "Username=certification_user;Password=certification_dev";

    public CertificationDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("CERTIFICATION_CONNECTION")
            ?? LocalDevelopmentConnection;

        var options = new DbContextOptionsBuilder<CertificationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new CertificationDbContext(options);
    }
}
