using Learning.Application.Abstractions;
using Learning.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        return services;
    }
}
