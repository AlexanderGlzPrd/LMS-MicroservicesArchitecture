using System.Net;
using BuildingBlocks.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using PaidEnrollment.Application.Abstractions;
using PaidEnrollment.Contracts.V1;
using PaidEnrollment.Infrastructure.Acl;
using PaidEnrollment.Infrastructure.Messaging;
using PaidEnrollment.Infrastructure.Persistence;
using PaidEnrollment.Infrastructure.Pricing;
using PaidEnrollment.Infrastructure.Saga;
using Polly;
using RabbitMQ.Client;
namespace PaidEnrollment.Infrastructure;
public static class DependencyInjection
{
    private static readonly string[] ReplyRoutingKeys =
    [
        "payment-authorized",
        "payment-declined",
        "payment-captured",
        "capture-failed",
        "authorization-voided",
        "payment-refunded",
        "refund-failed",
        "payment-status-reported",
        "enrollment-granted",
        "enrollment-rejected",
    ];

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration)
    {
        services.AddDbContext<PaidEnrollmentDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOutbox, OutboxWriter>();
        services.AddScoped<IInbox, InboxRecorder>();

        services.Configure<PurchaseOptions>(
            configuration.GetSection(PurchaseOptions.SectionName));
        services.Configure<EnrollmentOptions>(
            configuration.GetSection(EnrollmentOptions.SectionName));

        services.AddSingleton<IPurchaseAmounts, ConfiguredPurchaseAmounts>();

        services.Configure<SagaOptions>(configuration.GetSection(SagaOptions.SectionName));

        AddMessaging(services, configuration);

        AddEnrollmentClient(services);

        services.AddHostedService<PurchaseDriver>();
        services.AddHostedService<PurchaseReconciler>();

        return services;
    }

    private static void AddEnrollmentClient(IServiceCollection services)
    {
        services.AddHttpClient<IEnrollmentAccess, EnrollmentAccessClient>(
                (provider, client) =>
                {
                    var options = provider
                        .GetRequiredService<IOptions<EnrollmentOptions>>().Value;

                    client.BaseAddress = new Uri(EnsureTrailingSlash(options.BaseUrl));

                    client.Timeout = Timeout.InfiniteTimeSpan;
                })
            .AddResilienceHandler("enrollment", (pipeline, context) =>
            {
                var options = context.ServiceProvider
                    .GetRequiredService<IOptions<EnrollmentOptions>>().Value;

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
            bus.AddConsumer<PaymentReplyConsumer>();
            bus.AddConsumer<EnrollmentReplyConsumer>();

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

                ConfigureCommand<AuthorizePayment>(configurator);
                ConfigureCommand<CapturePayment>(configurator);
                ConfigureCommand<VoidAuthorization>(configurator);
                ConfigureCommand<RefundPayment>(configurator);
                ConfigureCommand<GetPaymentStatus>(configurator);
                ConfigureCommand<GrantEnrollmentForCapturedPayment>(configurator);

                configurator.ReceiveEndpoint("lms.paid-enrollment.saga-replies", endpoint =>
                {
                    endpoint.ConfigureConsumeTopology = false;
                    endpoint.Durable = true;
                    endpoint.AutoDelete = false;

                    foreach (var routingKey in ReplyRoutingKeys)
                    {
                        BindReply(endpoint, routingKey);
                    }

                    endpoint.UseMessageRetry(retry =>
                    {
                        retry.Intervals(
                            TimeSpan.FromMilliseconds(200),
                            TimeSpan.FromMilliseconds(500),
                            TimeSpan.FromSeconds(1));

                        retry.Ignore<InvalidSagaReplyMessageException>();
                        retry.Ignore<SagaCorrelationMismatchException>();
                    });

                    endpoint.ConfigureConsumer<PaymentReplyConsumer>(context);
                    endpoint.ConfigureConsumer<EnrollmentReplyConsumer>(context);
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

    private static void ConfigureCommand<TContract>(IRabbitMqBusFactoryConfigurator configurator)
        where TContract : class
    {
        configurator.Message<TContract>(message => message.SetEntityName("lms.saga.commands"));
        configurator.Publish<TContract>(publish => publish.ExchangeType = ExchangeType.Topic);
    }

    private static void BindReply(
        IRabbitMqReceiveEndpointConfigurator endpoint,
        string routingKey) =>
        endpoint.Bind("lms.saga.replies", binding =>
        {
            binding.ExchangeType = ExchangeType.Topic;
            binding.Durable = true;
            binding.AutoDelete = false;
            binding.RoutingKey = routingKey;
        });
}
