using Learning.Application.Abstractions;
using Learning.Infrastructure.Acl;
using Learning.Infrastructure.Messaging;
using Learning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
namespace Learning.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration)
    {
        services.AddDbContext<LearningDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ICourseProgressRepository, CourseProgressRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IInbox, InboxRecorder>();

        services.Configure<CourseAuthoringOptions>(
            configuration.GetSection(CourseAuthoringOptions.SectionName));

        services.AddHttpClient<ICurrentLessonSet, CourseAuthoringLessonSetClient>(
            (provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<CourseAuthoringOptions>>().Value;

                client.BaseAddress = new Uri(EnsureTrailingSlash(options.BaseUrl));
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });

        return services;
    }

    private static string EnsureTrailingSlash(string baseUrl) =>
        baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
}
