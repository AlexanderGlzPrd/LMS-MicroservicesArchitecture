using System.Globalization;
using Learning.Application.Abstractions;
using Learning.Domain.Progress;
using Learning.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
namespace Learning.Integration.Tests;

public sealed class LearningApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const int RetryAfterSeconds = 17;

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("learning")
        .WithUsername("learning_user")
        .WithPassword("learning_test")
        .Build();

    public ControllableCurrentLessonSet LessonSet { get; } = new();

    public string ConnectionString => container.GetConnectionString();

    public HttpClient CreateClientFor(Guid studentId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Student-Id", studentId.ToString());

        return client;
    }

    public HttpClient CreateAnonymousClient() => CreateClient();

    public Func<Task>? RaceHook { get; set; }

    public Func<Task>? TakeRaceHook()
    {
        var hook = RaceHook;
        RaceHook = null;

        return hook;
    }

    public async Task ResetAsync()
    {
        LessonSet.Reset();
        RaceHook = null;

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LearningDbContext>();

        await context.CourseProgresses.ExecuteDeleteAsync(CancellationToken.None);
    }

    public LearningDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LearningDbContext>()
            .UseNpgsql(container.GetConnectionString())
            .Options;

        return new LearningDbContext(options);
    }

    public async Task<int> CountProgressAsync(Guid studentId, Guid courseId)
    {
        var student = new StudentId(studentId);
        var course = new CourseId(courseId);

        await using var context = CreateContext();

        return await context.CourseProgresses.CountAsync(
            progress => progress.StudentId == student && progress.CourseId == course,
            CancellationToken.None);
    }

    public async Task<int> CountCompletedLessonsAsync(Guid studentId, Guid courseId)
    {
        var student = new StudentId(studentId);
        var course = new CourseId(courseId);

        await using var context = CreateContext();

        var progress = await context.CourseProgresses
            .Include(nameof(CourseProgress.CompletedLessons))
            .FirstOrDefaultAsync(
                candidate => candidate.StudentId == student && candidate.CourseId == course,
                CancellationToken.None);

        return progress?.CompletedLessons.Count ?? 0;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Learning", container.GetConnectionString());

        builder.UseSetting("Services:CourseAuthoring:BaseUrl", "http://course-authoring.invalid");
        builder.UseSetting(
            "Services:CourseAuthoring:RetryAfterSeconds",
            RetryAfterSeconds.ToString(CultureInfo.InvariantCulture));

        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICurrentLessonSet>();
            services.AddSingleton<ICurrentLessonSet>(LessonSet);

            services.RemoveAll<ICourseProgressRepository>();
            services.AddScoped<ICourseProgressRepository>(provider =>
                new RacingCourseProgressRepository(
                    new CourseProgressRepository(provider.GetRequiredService<LearningDbContext>()),
                    this));
        });
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await container.StartAsync();

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LearningDbContext>();

        await context.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await container.DisposeAsync();
        await base.DisposeAsync();
    }
}
