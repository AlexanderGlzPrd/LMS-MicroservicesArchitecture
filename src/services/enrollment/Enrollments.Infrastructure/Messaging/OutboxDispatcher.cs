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
        if (row.MessageType != OutboxContractMapper.StudentEnrolledType)
        {
            throw new UnsupportedOutboxMessageTypeException(row.Id, row.MessageType);
        }

        StudentEnrolled? contract;

        try
        {
            contract = JsonSerializer.Deserialize<StudentEnrolled>(
                row.Payload, OutboxSerialization.Options);
        }
        catch (JsonException exception)
        {
            throw new CorruptOutboxPayloadException(row.Id, exception);
        }

        if (contract is null
            || contract.StudentId == Guid.Empty
            || contract.CourseId == Guid.Empty
            || contract.OccurredAt == default)
        {
            throw new CorruptOutboxPayloadException(row.Id);
        }

        using var attempt = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        attempt.CancelAfter(TimeSpan.FromSeconds(options.Value.PublishTimeoutSeconds));
        await publishEndpoint.Publish(contract, context => context.MessageId = row.Id, attempt.Token);
    }

    private void RecordFailedAttempt(OutboxMessage message, Exception exception)
    {
        message.AttemptCount++;
        message.LastError = Truncate($"{exception.GetType().FullName}: {exception.Message}");
        message.LastAttemptAt = timeProvider.GetUtcNow();
    }

    private static string Truncate(string value) =>
        value.Length <= MaxLastErrorLength ? value : value[..MaxLastErrorLength];
}
