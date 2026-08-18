using System.Text.Json;
using BuildingBlocks.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PaymentProviderSim.Contracts.V1;
using PaymentProviderSim.Worker.Persistence;
namespace PaymentProviderSim.Worker.Messaging;
internal sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    private const int MaxLastErrorLength = 2000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(options.Value.PollingIntervalSeconds));

        try
        {
            do
            {
                try
                {
                    await DispatchBatchAsync(stoppingToken);
                }
                catch (Exception exception) when (
                    exception is not OperationCanceledException
                    || !stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(exception, "Fallo inesperado en el ciclo del Outbox.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    internal async Task DispatchBatchAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();

        var database = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var pending = await database.OutboxMessages
            .Where(message => message.PublishedAt == null)
            .OrderBy(message => message.Id)
            .Take(options.Value.BatchSize)
            .ToListAsync(stoppingToken);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var message in pending)
        {
            try
            {
                await PublishOneAsync(publishEndpoint, message, stoppingToken);

                message.PublishedAt = timeProvider.GetUtcNow();
            }
            catch (Exception exception)
                when (exception is UnsupportedOutboxMessageTypeException
                    or CorruptOutboxPayloadException)
            {
                RecordFailedAttempt(message, exception);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                await database.SaveChangesAsync(CancellationToken.None);

                return;
            }
            catch (Exception exception)
            {
                RecordFailedAttempt(message, exception);

                await database.SaveChangesAsync(CancellationToken.None);

                return;
            }
        }

        await database.SaveChangesAsync(CancellationToken.None);
    }

    private async Task PublishOneAsync(
        IPublishEndpoint publishEndpoint,
        OutboxMessage row,
        CancellationToken stoppingToken)
    {
        using var attempt = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        attempt.CancelAfter(TimeSpan.FromSeconds(options.Value.PublishTimeoutSeconds));

        var token = attempt.Token;

        if (row.MessageType == OutboxContractMapper.PaymentAuthorizedType)
        {
            await PublishAsync(publishEndpoint, row, Reconstruct<PaymentAuthorized>(
                row,
                contract => IsCorrelated(contract.PurchaseId, contract.PaymentId, contract.OccurredAt)
                    && contract.AuthorizedAt != default), token);

            return;
        }

        if (row.MessageType == OutboxContractMapper.PaymentDeclinedType)
        {
            await PublishAsync(publishEndpoint, row, Reconstruct<PaymentDeclined>(
                row,
                contract => IsCorrelated(contract.PurchaseId, contract.PaymentId, contract.OccurredAt)
                    && !string.IsNullOrWhiteSpace(contract.Reason)), token);

            return;
        }

        if (row.MessageType == OutboxContractMapper.PaymentCapturedType)
        {
            await PublishAsync(publishEndpoint, row, Reconstruct<PaymentCaptured>(
                row,
                contract => IsCorrelated(contract.PurchaseId, contract.PaymentId, contract.OccurredAt)
                    && contract.CapturedAt != default), token);

            return;
        }

        if (row.MessageType == OutboxContractMapper.CaptureFailedType)
        {
            await PublishAsync(publishEndpoint, row, Reconstruct<CaptureFailed>(
                row,
                contract => IsCorrelated(contract.PurchaseId, contract.PaymentId, contract.OccurredAt)
                    && !string.IsNullOrWhiteSpace(contract.Reason)), token);

            return;
        }

        if (row.MessageType == OutboxContractMapper.AuthorizationVoidedType)
        {
            await PublishAsync(publishEndpoint, row, Reconstruct<AuthorizationVoided>(
                row,
                contract => IsCorrelated(contract.PurchaseId, contract.PaymentId, contract.OccurredAt)
                    && contract.VoidedAt != default), token);

            return;
        }

        if (row.MessageType == OutboxContractMapper.PaymentRefundedType)
        {
            await PublishAsync(publishEndpoint, row, Reconstruct<PaymentRefunded>(
                row,
                contract => IsCorrelated(contract.PurchaseId, contract.PaymentId, contract.OccurredAt)
                    && contract.RefundedAt != default), token);

            return;
        }

        if (row.MessageType == OutboxContractMapper.RefundFailedType)
        {
            await PublishAsync(publishEndpoint, row, Reconstruct<RefundFailed>(
                row,
                contract => IsCorrelated(contract.PurchaseId, contract.PaymentId, contract.OccurredAt)
                    && !string.IsNullOrWhiteSpace(contract.Reason)), token);

            return;
        }

        if (row.MessageType == OutboxContractMapper.PaymentStatusReportedType)
        {
            await PublishAsync(publishEndpoint, row, Reconstruct<PaymentStatusReported>(
                row,
                contract => IsCorrelated(contract.PurchaseId, contract.PaymentId, contract.OccurredAt)
                    && !string.IsNullOrWhiteSpace(contract.Status)), token);

            return;
        }

        throw new UnsupportedOutboxMessageTypeException(row.Id, row.MessageType);
    }

    private static bool IsCorrelated(Guid purchaseId, Guid paymentId, DateTimeOffset occurredAt) =>
        purchaseId != Guid.Empty && paymentId != Guid.Empty && occurredAt != default;

    private static TContract Reconstruct<TContract>(
        OutboxMessage row,
        Func<TContract, bool> isUsable)
        where TContract : class
    {
        TContract? contract;

        try
        {
            contract = JsonSerializer.Deserialize<TContract>(
                row.Payload, OutboxSerialization.Options);
        }
        catch (JsonException exception)
        {
            throw new CorruptOutboxPayloadException(row.Id, exception);
        }

        if (contract is null || !isUsable(contract))
        {
            throw new CorruptOutboxPayloadException(row.Id);
        }

        return contract;
    }

    private static Task PublishAsync<TContract>(
        IPublishEndpoint publishEndpoint,
        OutboxMessage row,
        TContract contract,
        CancellationToken token)
        where TContract : class =>
        publishEndpoint.Publish(contract, context =>
        {
            context.MessageId = row.Id;
            context.SetRoutingKey(row.RoutingKey);
        }, token);

    private void RecordFailedAttempt(OutboxMessage message, Exception exception)
    {
        message.AttemptCount++;
        message.LastError = Truncate($"{exception.GetType().FullName}: {exception.Message}");
        message.LastAttemptAt = timeProvider.GetUtcNow();
    }

    private static string Truncate(string value) =>
        value.Length <= MaxLastErrorLength ? value : value[..MaxLastErrorLength];
}
