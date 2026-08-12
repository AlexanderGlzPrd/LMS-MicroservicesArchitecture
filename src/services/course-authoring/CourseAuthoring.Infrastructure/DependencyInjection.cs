using CourseAuthoring.Application.Abstractions;
using CourseAuthoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CourseAuthoring.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<CourseAuthoringDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ICatalogQueries, CatalogQueries>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
