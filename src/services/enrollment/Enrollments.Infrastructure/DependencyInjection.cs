using System.Net;
using BuildingBlocks.Messaging;
using Enrollments.Application.Abstractions;
using Enrollments.Contracts.V1;
using Enrollments.Infrastructure.Acl;
using Enrollments.Infrastructure.Messaging;
using Enrollments.Infrastructure.Persistence;
using MassTransit;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;

namespace Enrollments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration)
    {
        services.AddDbContext<EnrollmentsDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOutbox, OutboxWriter>();
        services.AddScoped<IInbox, InboxRecorder>();
        services.AddScoped<IPurchaseGrantLedger, PurchaseGrantLedger>();

        services.Configure<CourseAuthoringOptions>(
            configuration.GetSection(CourseAuthoringOptions.SectionName));

        AddMessaging(services, configuration);

        AddCourseAuthoringClient(services);

        return services;
    }

    private static void AddMessaging(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(
            configuration.GetSection(RabbitMqOptions.SectionName));
        services.Configure<OutboxOptions>(
            configuration.GetSection(OutboxOptions.SectionName));

        var rabbitMq = configuration.GetSection(RabbitMqOptions.SectionName)
            .Get<RabbitMqOptions>() ?? new RabbitMqOptions();

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<GrantEnrollmentForCapturedPaymentConsumer>();

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

                configurator.Message<StudentEnrolled>(
                    message => message.SetEntityName("lms.enrollment"));

                ConfigureReply<EnrollmentGranted>(configurator);
                ConfigureReply<EnrollmentRejected>(configurator);

                configurator.ReceiveEndpoint("lms.enrollment.enrollment-grants", endpoint =>
                {
                    endpoint.ConfigureConsumeTopology = false;
                    endpoint.Durable = true;
                    endpoint.AutoDelete = false;

                    endpoint.Bind("lms.saga.commands", binding =>
                    {
                        binding.ExchangeType = ExchangeType.Topic;
                        binding.Durable = true;
                        binding.AutoDelete = false;
                        binding.RoutingKey = "grant-enrollment-for-captured-payment";
                    });

                    endpoint.UseMessageRetry(retry =>
                    {
                        retry.Intervals(
                            TimeSpan.FromMilliseconds(200),
                            TimeSpan.FromMilliseconds(500),
                            TimeSpan.FromSeconds(1));

                        retry.Ignore<InvalidGrantEnrollmentMessageException>();
                    });

                    endpoint.ConfigureConsumer<GrantEnrollmentForCapturedPaymentConsumer>(context);
                });
            });
        });

        var outbox = configuration.GetSection(OutboxOptions.SectionName)
            .Get<OutboxOptions>() ?? new OutboxOptions();

        if (outbox.Enabled)
        {
            services.AddHostedService<OutboxDispatcher>();
        }
    }

    private static void AddCourseAuthoringClient(IServiceCollection services)
    {
        services.AddHttpClient<ICourseAvailability, CourseAuthoringCatalogClient>(
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

    private static ValueTask<bool> ShouldHandleTransient(Outcome<HttpResponseMessage> outcome) =>
        ValueTask.FromResult(
            outcome.Exception is HttpRequestException
            || outcome.Result is { StatusCode: HttpStatusCode.RequestTimeout }
            || (outcome.Result is { } response
                && (int)response.StatusCode >= 500 && (int)response.StatusCode <= 599));

    private static string EnsureTrailingSlash(string baseUrl) =>
        baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";

    private static void ConfigureReply<TContract>(IRabbitMqBusFactoryConfigurator configurator)
        where TContract : class
    {
        configurator.Message<TContract>(message => message.SetEntityName("lms.saga.replies"));
        configurator.Publish<TContract>(publish => publish.ExchangeType = ExchangeType.Topic);
    }
}
