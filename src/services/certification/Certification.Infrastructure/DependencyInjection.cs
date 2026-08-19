using System.Net;
using BuildingBlocks.Messaging;
using BuildingBlocks.Observability;
using Certification.Application.Abstractions;
using Certification.Application.Abstractions.Exceptions;
using Certification.Infrastructure.Acl;
using Certification.Infrastructure.Directory;
using Certification.Infrastructure.Identity;
using Certification.Infrastructure.Issuance;
using Certification.Infrastructure.Messaging;
using Certification.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
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

        services.Configure<KeycloakAdminOptions>(
            configuration.GetSection(KeycloakAdminOptions.SectionName));

        AddCourseAuthoringClient(services);
        AddStudentDirectoryClient(services);

        AddMessaging(services, configuration);
        AddIssuance(services, configuration);

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
                configurator.UseLmsConsumeCorrelation(context);

                configurator.Host(
                    rabbitMq.Host,
                    (ushort)rabbitMq.Port,
                    rabbitMq.VirtualHost,
                    host =>
                    {
                        host.Username(rabbitMq.Username);
                        host.Password(rabbitMq.Password);
                    });

                configurator.OverrideDefaultBusEndpointQueueName("lms.certification.bus");
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

    private static void AddIssuance(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CertificateIssuanceOptions>(
            configuration.GetSection(CertificateIssuanceOptions.SectionName));

        var issuance = configuration.GetSection(CertificateIssuanceOptions.SectionName)
            .Get<CertificateIssuanceOptions>() ?? new CertificateIssuanceOptions();

        if (issuance.Enabled)
        {
            services.AddHostedService<CertificateIssuanceDispatcher>();
        }
    }

    private static void AddCourseAuthoringClient(IServiceCollection services)
    {
        services.AddHttpClient<ICourseCatalog, CourseAuthoringCatalogClient>(
                (provider, client) =>
                {
                    var options = provider
                        .GetRequiredService<IOptions<CourseAuthoringOptions>>().Value;

                    client.BaseAddress = new Uri(EnsureTrailingSlash(options.BaseUrl));

                    client.Timeout = Timeout.InfiniteTimeSpan;
                })
            .AddResilienceHandler("course-authoring", (pipeline, context) =>
            {
                var options = context.ServiceProvider
                    .GetRequiredService<IOptions<CourseAuthoringOptions>>().Value;

                pipeline.AddTimeout(TimeSpan.FromSeconds(options.TotalTimeoutSeconds));

                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = options.RetryAttempts,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = false,
                    Delay = TimeSpan.FromMilliseconds(options.RetryBaseDelayMilliseconds),
                    ShouldHandle = args => ShouldHandleTransient(args.Outcome),
                });

                pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = options.CircuitBreakerFailureRatio,
                    SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreakerSamplingSeconds),
                    MinimumThroughput = options.CircuitBreakerMinimumThroughput,
                    BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakerBreakSeconds),
                    ShouldHandle = args => ShouldHandleTransient(args.Outcome),
                });
            });
    }

    private static void AddStudentDirectoryClient(IServiceCollection services)
    {
        services.AddHttpClient(ServiceTokenProvider.HttpClientName, client =>
                client.Timeout = Timeout.InfiniteTimeSpan)
            .AddResilienceHandler("keycloak-token", (pipeline, context) =>
            {
                var options = context.ServiceProvider
                    .GetRequiredService<IOptions<KeycloakAdminOptions>>().Value;

                pipeline.AddTimeout(TimeSpan.FromSeconds(options.TotalTimeoutSeconds));

                // Sin circuit breaker propio: el fallo ya se traduce en el ACL que lo usa
                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = options.RetryAttempts,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = false,
                    Delay = TimeSpan.FromMilliseconds(options.RetryBaseDelayMilliseconds),
                    ShouldHandle = args => ShouldHandleTransient(args.Outcome),
                });
            });

        services.AddSingleton<ServiceTokenProvider>();

        services.AddHttpClient<IStudentDirectory, KeycloakStudentDirectory>(
                (provider, client) =>
                {
                    var options = provider
                        .GetRequiredService<IOptions<KeycloakAdminOptions>>().Value;

                    client.BaseAddress = new Uri(EnsureTrailingSlash(options.AdminBaseUrl));

                    client.Timeout = Timeout.InfiniteTimeSpan;
                })
            .AddResilienceHandler("keycloak-admin", (pipeline, context) =>
            {
                var options = context.ServiceProvider
                    .GetRequiredService<IOptions<KeycloakAdminOptions>>().Value;

                pipeline.AddTimeout(TimeSpan.FromSeconds(options.TotalTimeoutSeconds));

                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = options.RetryAttempts,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = false,
                    Delay = TimeSpan.FromMilliseconds(options.RetryBaseDelayMilliseconds),
                    ShouldHandle = args => ShouldHandleTransient(args.Outcome),
                });

                pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = options.CircuitBreakerFailureRatio,
                    SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreakerSamplingSeconds),
                    MinimumThroughput = options.CircuitBreakerMinimumThroughput,
                    BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakerBreakSeconds),
                    ShouldHandle = args => ShouldHandleTransient(args.Outcome),
                });
            });
    }

    private static ValueTask<bool> ShouldHandleTransient(Outcome<HttpResponseMessage> outcome) =>
        ValueTask.FromResult(
            outcome.Exception is HttpRequestException
            || outcome.Result is { StatusCode: HttpStatusCode.RequestTimeout }
            || (outcome.Result is { } response
                && (int)response.StatusCode >= 500 && (int)response.StatusCode <= 599));

    private static string EnsureTrailingSlash(string baseUrl) =>
        baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
}
