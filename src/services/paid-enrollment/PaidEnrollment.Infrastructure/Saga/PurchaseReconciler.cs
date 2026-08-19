using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaidEnrollment.Application.Abstractions;
using PaidEnrollment.Application.Purchases.Workflow;
namespace PaidEnrollment.Infrastructure.Saga;
internal sealed class PurchaseReconciler(
    IServiceScopeFactory scopeFactory,
    IOptions<SagaOptions> options,
    TimeProvider timeProvider,
    ILogger<PurchaseReconciler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(options.Value.ReconciliationIntervalSeconds));

        try
        {
            do
            {
                try
                {
                    await ReconcileBatchAsync(stoppingToken);
                }
                catch (Exception exception) when (
                    exception is not OperationCanceledException
                    || !stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(exception, "Fallo inesperado en el ciclo de reconciliacion.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    internal async Task ReconcileBatchAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();

        var purchases = scope.ServiceProvider.GetRequiredService<IPurchaseRepository>();
        var reconciliation = scope.ServiceProvider.GetRequiredService<PurchaseReconciliation>();

        var expiredBefore = timeProvider.GetUtcNow()
            - TimeSpan.FromSeconds(options.Value.StepTimeoutSeconds);

        var expired = await purchases.ListExpiredAsync(
            expiredBefore, options.Value.BatchSize, stoppingToken);

        foreach (var purchase in expired)
        {
            var before = purchase.Status;

            try
            {
                var after = await reconciliation.ReconcileAsync(
                    purchase, options.Value.MaxReconciliationAttempts, stoppingToken);

                logger.LogInformation(
                    "La compra {PurchaseId} vencio en {Before} y se reconcilio hacia {After} "
                    + "con {Attempts} intentos consumidos.",
                    purchase.Id.Value,
                    before,
                    after,
                    purchase.StepAttempts);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "No se pudo reconciliar la compra {PurchaseId} desde {Status}.",
                    purchase.Id.Value,
                    before);

                return;
            }
        }
    }
}