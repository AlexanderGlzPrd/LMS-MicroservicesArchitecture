using BuildingBlocks.Messaging;
using Certification.Application.Abstractions;
using Certification.Application.Abstractions.Exceptions;
using Certification.Infrastructure.Acl;
using Certification.Infrastructure.Directory;
using Certification.Infrastructure.Issuance;
using Certification.Infrastructure.Messaging;
using Certification.Infrastructure.Persistence;
using MassTransit;
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
        services.AddScoped<IInbox, InboxRecorder>();
        services.AddScoped<IPendingCertificateIssuances, PendingCertificateIssuanceStore>();

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

        AddMessaging(services, configuration);

        return services;
    }

    private static void AddMessaging(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(
            configuration.GetSection(RabbitMqOptions.SectionName));

        var rabbitMq = configuration.GetSection(RabbitMqOptions.SectionName)
            .Get<RabbitMqOptions>() ?? new RabbitMqOptions();

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<CourseCompletedConsumer>();

            bus.UsingRabbitMq((context, configurator) =>
            {
                configurator.Host(
                    rabbitMq.Host,
                    (ushort)rabbitMq.Port,
                    rabbitMq.VirtualHost,
                    host =>
                    {
                        host.Username(rabbitMq.Username);
                        host.Password(rabbitMq.Password);
                    });

                configurator.ReceiveEndpoint("lms.certification.course-completed", endpoint =>
                {
                    endpoint.ConfigureConsumeTopology = false;
                    endpoint.Durable = true;
                    endpoint.AutoDelete = false;

                    endpoint.Bind("lms.learning", binding =>
                    {
                        binding.ExchangeType = "fanout";
                        binding.Durable = true;
                        binding.AutoDelete = false;
                    });

                    endpoint.UseMessageRetry(retry =>
                    {
                        retry.Intervals(
                            TimeSpan.FromMilliseconds(200),
                            TimeSpan.FromMilliseconds(500),
                            TimeSpan.FromSeconds(1));

                        retry.Ignore<InvalidCourseCompletedMessageException>();
                        retry.Ignore<ContradictoryCourseCompletionException>();
                    });

                    endpoint.ConfigureConsumer<CourseCompletedConsumer>(context);
                });
            });
        });
    }

    private static string EnsureTrailingSlash(string baseUrl) =>
        baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
}
