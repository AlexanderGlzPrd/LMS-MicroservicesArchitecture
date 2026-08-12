using CourseAuthoring.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace CourseAuthoring.Integration.Tests;

public sealed class CourseAuthoringApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("course_authoring")
        .WithUsername("course_authoring_user")
        .WithPassword("course_authoring_test")
        .Build();

    public HttpClient CreateClientFor(Guid instructorId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Instructor-Id", instructorId.ToString());

        return client;
    }

    /// <summary>Cliente sin cabecera de instructor: el del publico.</summary>
    public HttpClient CreateAnonymousClient() => CreateClient();

    public async Task ResetAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CourseAuthoringDbContext>();

        await context.Courses.ExecuteDeleteAsync(CancellationToken.None);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:CourseAuthoring", container.GetConnectionString());

        // Development es lo que expone /openapi; el resto de la tuberia es identica.
        builder.UseEnvironment("Development");
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await container.StartAsync();

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CourseAuthoringDbContext>();

        await context.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await container.DisposeAsync();
        await base.DisposeAsync();
    }
}
