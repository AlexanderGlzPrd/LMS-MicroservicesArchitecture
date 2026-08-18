using BuildingBlocks.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaidEnrollment.Application.Abstractions;
using PaidEnrollment.Contracts.V1;
using PaidEnrollment.Infrastructure.Messaging;
using PaidEnrollment.Infrastructure.Persistence;
using RabbitMQ.Client;
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

        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOutbox, OutboxWriter>();
        services.AddScoped<IInbox, InboxRecorder>();

        AddMessaging(services, configuration);

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
}
