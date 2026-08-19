using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaidEnrollment.Application.Abstractions;
using PaidEnrollment.Application.Purchases.Workflow;
namespace PaidEnrollment.Infrastructure.Saga;
internal sealed class PurchaseDriver(
    IServiceScopeFactory scopeFactory,
    IOptions<SagaOptions> options,
    ILogger<PurchaseDriver> logger) : BackgroundService
{
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

        var pending = await purchases.ListDrivableAsync(options.Value.BatchSize, stoppingToken);

        foreach (var purchase in pending)
        {
            var before = purchase.Status;

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
}
