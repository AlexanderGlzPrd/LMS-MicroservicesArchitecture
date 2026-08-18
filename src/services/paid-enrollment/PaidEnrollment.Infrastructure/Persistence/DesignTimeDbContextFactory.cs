using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace PaidEnrollment.Infrastructure.Persistence;

internal sealed class DesignTimeDbContextFactory
    : IDesignTimeDbContextFactory<PaidEnrollmentDbContext>
{
    private const string LocalDevelopmentConnection =
        "Host=localhost;Port=5432;Database=purchase;" +
        "Username=purchase_user;Password=purchase_dev";

    public PaidEnrollmentDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("PURCHASE_CONNECTION")
            ?? LocalDevelopmentConnection;

        var options = new DbContextOptionsBuilder<PaidEnrollmentDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new PaidEnrollmentDbContext(options);
    }
}