using System.Diagnostics;
using BuildingBlocks.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaidEnrollment.Application.Abstractions;
using PaidEnrollment.Application.Purchases.Workflow;
using PaidEnrollment.Domain.Purchases;
using PaidEnrollment.Infrastructure.Persistence;
using PaidEnrollment.Infrastructure.Persistence.Configurations;
namespace PaidEnrollment.Infrastructure.Saga;
internal sealed class PurchaseDriver(
    IServiceScopeFactory scopeFactory,
    IOptions<SagaOptions> options,
    ILogger<PurchaseDriver> logger) : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new("paid-enrollment");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(options.Value.DriverIntervalSeconds));

        try
        {
            do
            {
                try
                {
                    await DriveBatchAsync(stoppingToken);
                }
                catch (Exception exception) when (
                    exception is not OperationCanceledException
                    || !stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(exception, "Fallo inesperado en el ciclo del driver.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    internal async Task DriveBatchAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();

        var purchases = scope.ServiceProvider.GetRequiredService<IPurchaseRepository>();
        var advancer = scope.ServiceProvider.GetRequiredService<PurchaseAdvancer>();
        var database = scope.ServiceProvider.GetRequiredService<PaidEnrollmentDbContext>();

        var pending = await purchases.ListDrivableAsync(options.Value.BatchSize, stoppingToken);

        foreach (var purchase in pending)
        {
            var before = purchase.Status;

            using var activity = StartPurchaseActivity(database, purchase);

            Describe(activity, purchase);

            using var logScope = logger.BeginScope(Scope(purchase));

            try
            {
                var after = await advancer.AdvanceAsync(
                    purchase, options.Value.MaxPreCheckAttempts, stoppingToken);

                if (after != before)
                {
                    logger.LogInformation(
                        "La compra {PurchaseId} paso de {Before} a {After}.",
                        purchase.Id.Value,
                        before,
                        after);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "No se pudo avanzar la compra {PurchaseId} desde {Status}.",
                    purchase.Id.Value,
                    before);

                return;
            }
        }
    }

    private static void Describe(Activity? activity, Purchase purchase)
    {
        activity?.SetTag("lms.purchase.id", purchase.Id.Value);
        activity?.SetTag("lms.purchase.state", purchase.Status.ToString());

        activity?.SetTag("lms.payment.id", purchase.PaymentId.Value);
    }

    private static Dictionary<string, object?> Scope(Purchase purchase) => new()
    {
        ["PurchaseId"] = purchase.Id.Value,
        ["PaymentId"] = purchase.PaymentId.Value,
        ["PurchaseState"] = purchase.Status.ToString(),
        ["TraceId"] = Activity.Current?.TraceId.ToString(),
    };

    private Activity? StartPurchaseActivity(PaidEnrollmentDbContext database, Purchase purchase)
    {
        var traceContext = database.Entry(purchase)
            .Property<string?>(PurchaseConfiguration.TraceContextProperty)
            .CurrentValue;

        if (OutboxTraceContext.TryRestore(traceContext, out var origin))
        {
            return ActivitySource.StartActivity(
                "saga drive", ActivityKind.Internal, origin);
        }

        logger.LogWarning(
            "La compra {PurchaseId} no lleva un contexto de traza utilizable; "
            + "se avanza bajo una actividad raiz nueva.",
            purchase.Id.Value);

        return ActivitySource.StartActivity("saga drive", ActivityKind.Internal);
    }
}
