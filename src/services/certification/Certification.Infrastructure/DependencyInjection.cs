using Certification.Application.Abstractions;
using Certification.Infrastructure.Acl;
using Certification.Infrastructure.Directory;
using Certification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
namespace Certification.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration)
    {
        services.AddDbContext<CertificationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ICertificateRepository, CertificateRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.Configure<CourseAuthoringOptions>(
            configuration.GetSection(CourseAuthoringOptions.SectionName));

        services.Configure<StudentDirectoryOptions>(
            configuration.GetSection(StudentDirectoryOptions.SectionName));

        services.AddScoped<IStudentDirectory, ConfiguredStudentDirectory>();

        services.AddHttpClient<ICourseCatalog, CourseAuthoringCatalogClient>(
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
