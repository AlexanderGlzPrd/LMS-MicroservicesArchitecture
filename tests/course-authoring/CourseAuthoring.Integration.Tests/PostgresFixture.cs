using CourseAuthoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace CourseAuthoring.Integration.Tests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("course_authoring")
        .WithUsername("course_authoring_user")
        .WithPassword("course_authoring_test")
        .Build();

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    public string ConnectionString => container.GetConnectionString();

    public CourseAuthoringDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CourseAuthoringDbContext>()
            .UseNpgsql(container.GetConnectionString())
            .Options;

        return new CourseAuthoringDbContext(options);
    }
}
