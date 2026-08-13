using Enrollments.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Testcontainers.PostgreSql;

namespace Enrollments.Integration.Tests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("enrollment")
        .WithUsername("enrollment_user")
        .WithPassword("enrollment_test")
        .Build();

    public string ConnectionString => container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    public EnrollmentsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EnrollmentsDbContext>()
            .UseNpgsql(container.GetConnectionString())
            .Options;

        return new EnrollmentsDbContext(options);
    }
}
