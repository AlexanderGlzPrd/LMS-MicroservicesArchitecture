using System.Net;
using BuildingBlocks.Messaging;
using Learning.Application.Abstractions;
using Learning.Contracts.V1;
using Learning.Infrastructure.Acl;
using Learning.Infrastructure.Messaging;
using Learning.Infrastructure.Persistence;
using Learning.Infrastructure.Projection;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
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
        services.AddScoped<ICourseProgressReadModel, CourseProgressReadModel>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IInbox, InboxRecorder>();

        services.Configure<CourseAuthoringOptions>(
            configuration.GetSection(CourseAuthoringOptions.SectionName));

        AddMessaging(services, configuration);
        AddProjection(services, configuration);

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

                configurator.OverrideDefaultBusEndpointQueueName("lms.learning.bus");
                configurator.Message<CourseCompleted>(
                    message => message.SetEntityName("lms.learning"));

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

        var outbox = configuration.GetSection(OutboxOptions.SectionName)
            .Get<OutboxOptions>() ?? new OutboxOptions();

        if (outbox.Enabled)
        {
            services.AddHostedService<OutboxDispatcher>();
        }
    }

    private static void AddProjection(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ProjectionOptions>(
            configuration.GetSection(ProjectionOptions.SectionName));

        var projection = configuration.GetSection(ProjectionOptions.SectionName)
            .Get<ProjectionOptions>() ?? new ProjectionOptions();

        if (projection.Enabled)
        {
            services.AddHostedService<ProjectionDispatcher>();
        }
    }

    private static void AddCourseAuthoringClient(IServiceCollection services)
    {
        services.AddHttpClient<ICurrentLessonSet, CourseAuthoringLessonSetClient>(
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
}
