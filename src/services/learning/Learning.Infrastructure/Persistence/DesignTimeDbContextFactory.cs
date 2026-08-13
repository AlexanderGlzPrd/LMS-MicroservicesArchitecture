using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace Learning.Infrastructure.Persistence;

internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LearningDbContext>
{
    private const string LocalDevelopmentConnection =
        "Host=localhost;Port=5432;Database=learning;" +
        "Username=learning_user;Password=learning_dev";

    public LearningDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("LEARNING_CONNECTION")
            ?? LocalDevelopmentConnection;

        var options = new DbContextOptionsBuilder<LearningDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new LearningDbContext(options);
    }
}
