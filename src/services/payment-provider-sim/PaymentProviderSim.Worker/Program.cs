using BuildingBlocks.Messaging;
using BuildingBlocks.Observability;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using PaymentProviderSim.Contracts.V1;
using PaymentProviderSim.Worker.Messaging;
using PaymentProviderSim.Worker.Payments;
using PaymentProviderSim.Worker.Persistence;
using PaymentProviderSim.Worker.Rules;
using PaymentProviderSim.Worker.Time;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);
builder.AddLmsObservability("payment-provider-sim");

var connectionString = builder.Configuration.GetConnectionString("PaymentProviderSim")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexion 'PaymentProviderSim' en la configuracion.");

if (string.IsNullOrWhiteSpace(
        builder.Configuration[$"{RabbitMqOptions.SectionName}:Host"]))
{
    throw new InvalidOperationException(
        $"Falta '{RabbitMqOptions.SectionName}:Host' en la configuracion.");
}

builder.Services.AddSingleton<TimeProvider>(new MicrosecondTimeProvider(TimeProvider.System));

builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.Configure<SimulatorOptions>(
    builder.Configuration.GetSection(SimulatorOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.Configure<OutboxOptions>(
    builder.Configuration.GetSection(OutboxOptions.SectionName));

builder.Services.AddSingleton<SimulatorRules>();
builder.Services.AddScoped<InboxRecorder>();
builder.Services.AddScoped<OutboxWriter>();
builder.Services.AddScoped<UnitOfWork>();
builder.Services.AddScoped<PaymentService>();

var rabbitMq = builder.Configuration.GetSection(RabbitMqOptions.SectionName)
    .Get<RabbitMqOptions>() ?? new RabbitMqOptions();

builder.Services.AddMassTransit(bus =>
{
    bus.AddConsumer<PaymentCommandConsumer>();

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

        configurator.OverrideDefaultBusEndpointQueueName("lms.payment-provider-sim.bus");
        ConfigureReply<PaymentAuthorized>(configurator);
        ConfigureReply<PaymentDeclined>(configurator);
        ConfigureReply<PaymentCaptured>(configurator);
        ConfigureReply<CaptureFailed>(configurator);
        ConfigureReply<AuthorizationVoided>(configurator);
        ConfigureReply<PaymentRefunded>(configurator);
        ConfigureReply<RefundFailed>(configurator);
        ConfigureReply<PaymentStatusReported>(configurator);

        configurator.ReceiveEndpoint("lms.payment-provider-sim.payment-commands", endpoint =>
        {
            endpoint.ConfigureConsumeTopology = false;
            endpoint.Durable = true;
            endpoint.AutoDelete = false;

            BindCommand(endpoint, "authorize-payment");
            BindCommand(endpoint, "capture-payment");
            BindCommand(endpoint, "void-authorization");
            BindCommand(endpoint, "refund-payment");
            BindCommand(endpoint, "get-payment-status");

            endpoint.UseMessageRetry(retry =>
            {
                retry.Intervals(
                    TimeSpan.FromMilliseconds(200),
                    TimeSpan.FromMilliseconds(500),
                    TimeSpan.FromSeconds(1));

                retry.Ignore<InvalidPaymentCommandMessageException>();
                retry.Ignore<PaymentIdCollisionException>();
            });

            endpoint.ConfigureConsumer<PaymentCommandConsumer>(context);
        });
    });
});

var outbox = builder.Configuration.GetSection(OutboxOptions.SectionName)
    .Get<OutboxOptions>() ?? new OutboxOptions();

if (outbox.Enabled)
{
    builder.Services.AddHostedService<OutboxDispatcher>();
}

builder.Services.AddHealthChecks()
    .AddDbContextCheck<PaymentsDbContext>();

var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = registration => !registration.Tags.Contains("masstransit"),
});

app.Run();

static void ConfigureReply<TContract>(IRabbitMqBusFactoryConfigurator configurator)
    where TContract : class
{
    configurator.Message<TContract>(message => message.SetEntityName("lms.saga.replies"));
    configurator.Publish<TContract>(publish => publish.ExchangeType = ExchangeType.Topic);
}

static void BindCommand(IRabbitMqReceiveEndpointConfigurator endpoint, string routingKey) =>
    endpoint.Bind("lms.saga.commands", binding =>
    {
        binding.ExchangeType = ExchangeType.Topic;
        binding.Durable = true;
        binding.AutoDelete = false;
        binding.RoutingKey = routingKey;
    });
