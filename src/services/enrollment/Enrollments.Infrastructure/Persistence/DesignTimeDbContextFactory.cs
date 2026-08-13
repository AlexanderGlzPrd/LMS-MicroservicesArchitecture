using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Enrollments.Infrastructure.Persistence;
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<EnrollmentsDbContext>
{
    private const string LocalDevelopmentConnection =
        "Host=localhost;Port=5432;Database=enrollment;" +
        "Username=enrollment_user;Password=enrollment_dev";

    public EnrollmentsDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ENROLLMENT_CONNECTION")
            ?? LocalDevelopmentConnection;

        var options = new DbContextOptionsBuilder<EnrollmentsDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new EnrollmentsDbContext(options);
    }
}
