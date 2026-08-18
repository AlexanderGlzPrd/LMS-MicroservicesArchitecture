using System.Text.Json;
using BuildingBlocks.Messaging;
using Enrollments.Contracts.V1;
using Enrollments.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace Enrollments.Infrastructure.Messaging;
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

        var database = scope.ServiceProvider.GetRequiredService<EnrollmentsDbContext>();
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

        if (row.MessageType == OutboxContractMapper.StudentEnrolledType)
        {
            await PublishAsync(publishEndpoint, row, Reconstruct<StudentEnrolled>(
                row,
                contract => contract.StudentId != Guid.Empty
                    && contract.CourseId != Guid.Empty
                    && contract.OccurredAt != default), token);

            return;
        }

        if (row.MessageType == OutboxContractMapper.EnrollmentGrantedType)
        {
            await PublishAsync(publishEndpoint, row, Reconstruct<EnrollmentGranted>(
                row,
                contract => IsCorrelated(
                        contract.PurchaseId, contract.StudentId, contract.CourseId,
                        contract.OccurredAt)
                    && !string.IsNullOrWhiteSpace(contract.Outcome)
                    && !string.IsNullOrWhiteSpace(contract.Origin)), token);

            return;
        }

        if (row.MessageType == OutboxContractMapper.EnrollmentRejectedType)
        {
            await PublishAsync(publishEndpoint, row, Reconstruct<EnrollmentRejected>(
                row,
                contract => IsCorrelated(
                        contract.PurchaseId, contract.StudentId, contract.CourseId,
                        contract.OccurredAt)
                    && !string.IsNullOrWhiteSpace(contract.Reason)), token);

            return;
        }

        throw new UnsupportedOutboxMessageTypeException(row.Id, row.MessageType);
    }

    private static bool IsCorrelated(
        Guid purchaseId, Guid studentId, Guid courseId, DateTimeOffset occurredAt) =>
        purchaseId != Guid.Empty
        && studentId != Guid.Empty
        && courseId != Guid.Empty
        && occurredAt != default;

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

            if (row.RoutingKey is not null)
            {
                context.SetRoutingKey(row.RoutingKey);
            }
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
