using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaidEnrollment.Infrastructure.Persistence;
namespace PaidEnrollment.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration)
    {
        services.AddDbContext<PaidEnrollmentDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}