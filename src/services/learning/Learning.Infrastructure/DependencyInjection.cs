using Learning.Application.Abstractions;
using Learning.Infrastructure.Acl;
using Learning.Infrastructure.Messaging;
using Learning.Infrastructure.Persistence;
using MassTransit;
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

        AddMessaging(services, configuration);

        services.AddHttpClient<ICurrentLessonSet, CourseAuthoringLessonSetClient>(
            (provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<CourseAuthoringOptions>>().Value;

                client.BaseAddress = new Uri(EnsureTrailingSlash(options.BaseUrl));
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });

        return services;
    }

    // Learning no configura WaitUntilStarted: la API debe arrancar con el broker caido.
    private static void AddMessaging(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(
            configuration.GetSection(RabbitMqOptions.SectionName));

        var rabbitMq = configuration.GetSection(RabbitMqOptions.SectionName)
            .Get<RabbitMqOptions>() ?? new RabbitMqOptions();

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<StudentEnrolledConsumer>();

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

                // Endpoint explicito: los nombres son los que decidio ADR-T07, no los
                // que deduzca la convencion de la biblioteca del namespace del tipo.
                configurator.ReceiveEndpoint("lms.learning.student-enrolled", endpoint =>
                {
                    endpoint.ConfigureConsumeTopology = false;
                    endpoint.Durable = true;
                    endpoint.AutoDelete = false;

                    endpoint.Bind("lms.enrollment", binding =>
                    {
                        binding.ExchangeType = "fanout";
                        binding.Durable = true;
                        binding.AutoDelete = false;
                    });

                    // 1 intento + 3 reintentos. Un error de contrato es funcional y va
                    // directo a la _error queue, sin reintentar.
                    endpoint.UseMessageRetry(retry =>
                    {
                        retry.Intervals(
                            TimeSpan.FromMilliseconds(200),
                            TimeSpan.FromMilliseconds(500),
                            TimeSpan.FromSeconds(1));

                        retry.Ignore<InvalidStudentEnrolledMessageException>();
                    });

                    endpoint.ConfigureConsumer<StudentEnrolledConsumer>(context);
                });
            });
        });
    }

    private static string EnsureTrailingSlash(string baseUrl) =>
        baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
}
