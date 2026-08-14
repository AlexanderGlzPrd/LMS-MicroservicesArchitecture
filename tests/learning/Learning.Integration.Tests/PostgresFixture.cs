using Learning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
namespace Learning.Integration.Tests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("learning")
        .WithUsername("learning_user")
        .WithPassword("learning_test")
        .Build();

    public string ConnectionString => container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    public LearningDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LearningDbContext>()
            .UseNpgsql(container.GetConnectionString())
            .Options;

        return new LearningDbContext(options);
    }
}
