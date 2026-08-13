using System.Globalization;

using Enrollments.Application.Abstractions;
using Enrollments.Domain.Enrollments;
using Enrollments.Infrastructure.Persistence;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Testcontainers.PostgreSql;

namespace Enrollments.Integration.Tests;

public sealed class EnrollmentsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const int RetryAfterSeconds = 17;

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("enrollment")
        .WithUsername("enrollment_user")
        .WithPassword("enrollment_test")
        .Build();

    public ControllableCourseAvailability CourseAvailability { get; } = new();

    public HttpClient CreateClientFor(Guid studentId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Student-Id", studentId.ToString());

        return client;
    }

    public HttpClient CreateAnonymousClient() => CreateClient();

    public async Task ResetAsync()
    {
        CourseAvailability.Reset();

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentsDbContext>();

        await context.Enrollments.ExecuteDeleteAsync(CancellationToken.None);
    }

    public async Task<int> CountEnrollmentsAsync(Guid studentId, Guid courseId)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentsDbContext>();

        var student = new StudentId(studentId);
        var course = new CourseId(courseId);

        return await context.Enrollments.CountAsync(
            enrollment => enrollment.StudentId == student && enrollment.CourseId == course,
            CancellationToken.None);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Enrollment", container.GetConnectionString());

        builder.UseSetting("Services:CourseAuthoring:BaseUrl", "http://course-authoring.invalid");
        builder.UseSetting(
            "Services:CourseAuthoring:RetryAfterSeconds",
            RetryAfterSeconds.ToString(CultureInfo.InvariantCulture));

        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICourseAvailability>();
            services.AddSingleton<ICourseAvailability>(CourseAvailability);
        });
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await container.StartAsync();

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EnrollmentsDbContext>();

        await context.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await container.DisposeAsync();
        await base.DisposeAsync();
    }
}
