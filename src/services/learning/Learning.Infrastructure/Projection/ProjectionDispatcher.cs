using System.Text.Json;
using Learning.Domain.Progress;
using Learning.Domain.Progress.Events;
using Learning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace Learning.Infrastructure.Projection;
internal sealed class ProjectionDispatcher(
    IServiceScopeFactory scopeFactory,
    IOptions<ProjectionOptions> options,
    TimeProvider timeProvider,
    ILogger<ProjectionDispatcher> logger) : BackgroundService
{
    private const int MaxLastErrorLength = 2000;

    private static readonly string StartedType = typeof(CourseProgressStarted).FullName!;
    private static readonly string LessonCompletedType = typeof(LessonCompleted).FullName!;
    private static readonly string CompletedType = typeof(CourseProgressCompleted).FullName!;

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
                    await ApplyBatchAsync(stoppingToken);
                }
                catch (Exception exception) when (
                    exception is not OperationCanceledException
                    || !stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(exception, "Fallo inesperado en el ciclo de proyeccion.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    internal async Task ApplyBatchAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();

        var database = scope.ServiceProvider.GetRequiredService<LearningDbContext>();

        var pending = await database.ProgressEvents
            .Where(progressEvent => progressEvent.AppliedAt == null)
            .OrderBy(progressEvent => progressEvent.SequenceNo)
            .Take(options.Value.BatchSize)
            .ToListAsync(stoppingToken);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var progressEvent in pending)
        {
            try
            {
                await ApplyOneAsync(database, progressEvent, stoppingToken);

                progressEvent.AppliedAt = timeProvider.GetUtcNow();

                await database.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                database.ChangeTracker.Clear();

                await RecordFailedAttemptAsync(database, progressEvent.Id, exception);

                return;
            }
        }
    }

    private async Task ApplyOneAsync(
        LearningDbContext database,
        ProgressEvent progressEvent,
        CancellationToken stoppingToken)
    {
        if (progressEvent.EventType == StartedType)
        {
            var started = Deserialize<CourseProgressStarted>(progressEvent);

            database.CourseProgressViews.Add(new CourseProgressViewRow
            {
                StudentId = progressEvent.StudentId,
                CourseId = progressEvent.CourseId,
                Status = CourseProgressStatus.InProgress.ToString(),
                StartedAt = started.OccurredAt,
                CompletedAt = null,
                CompletedLessonIds = [],
                CompletedLessonDates = [],
                CompletedLessonCount = 0,
                TotalLessonCount = null,
            });

            return;
        }

        var row = await LoadRowAsync(database, progressEvent, stoppingToken);

        if (progressEvent.EventType == LessonCompletedType)
        {
            ApplyLessonCompleted(row, Deserialize<LessonCompleted>(progressEvent));

            return;
        }

        if (progressEvent.EventType == CompletedType)
        {
            var completed = Deserialize<CourseProgressCompleted>(progressEvent);

            row.Status = CourseProgressStatus.Completed.ToString();
            row.CompletedAt = completed.OccurredAt;
            row.TotalLessonCount = completed.ObservedTotalLessonCount;

            return;
        }

        throw new UnsupportedProgressEventTypeException(progressEvent.Id, progressEvent.EventType);
    }

    private static void ApplyLessonCompleted(CourseProgressViewRow row, LessonCompleted lesson)
    {
        row.TotalLessonCount = lesson.ObservedTotalLessonCount;

        if (row.CompletedLessonIds.Contains(lesson.LessonId))
        {
            return;
        }

        var ids = new List<Guid>(row.CompletedLessonIds);
        var dates = new List<DateTimeOffset>(row.CompletedLessonDates);

        var position = 0;

        while (position < ids.Count
            && (dates[position] < lesson.OccurredAt
                || (dates[position] == lesson.OccurredAt
                    && ids[position].CompareTo(lesson.LessonId) < 0)))
        {
            position++;
        }

        ids.Insert(position, lesson.LessonId);
        dates.Insert(position, lesson.OccurredAt);

        row.CompletedLessonIds = ids;
        row.CompletedLessonDates = dates;
        row.CompletedLessonCount = ids.Count;
    }

    private static async Task<CourseProgressViewRow> LoadRowAsync(
        LearningDbContext database,
        ProgressEvent progressEvent,
        CancellationToken stoppingToken)
    {
        var row = await database.CourseProgressViews.FirstOrDefaultAsync(
            view => view.StudentId == progressEvent.StudentId
                && view.CourseId == progressEvent.CourseId,
            stoppingToken);

        return row ?? throw new MissingCourseProgressViewException(
            progressEvent.Id, progressEvent.StudentId, progressEvent.CourseId);
    }

    private static TEvent Deserialize<TEvent>(ProgressEvent progressEvent)
        where TEvent : class
    {
        TEvent? domainEvent;

        try
        {
            domainEvent = JsonSerializer.Deserialize<TEvent>(
                progressEvent.Payload, ProgressEventSerialization.Options);
        }
        catch (JsonException exception)
        {
            throw new CorruptProgressEventPayloadException(progressEvent.Id, exception);
        }

        return domainEvent ?? throw new CorruptProgressEventPayloadException(progressEvent.Id);
    }

    private async Task RecordFailedAttemptAsync(
        LearningDbContext database,
        Guid progressEventId,
        Exception exception)
    {
        using var diagnostics = new CancellationTokenSource(
            TimeSpan.FromSeconds(options.Value.DiagnosticsTimeoutSeconds));

        try
        {
            var row = await database.ProgressEvents.FirstAsync(
                progressEvent => progressEvent.Id == progressEventId, diagnostics.Token);

            row.AttemptCount++;
            row.LastError = Truncate($"{exception.GetType().FullName}: {exception.Message}");
            row.LastAttemptAt = timeProvider.GetUtcNow();

            await database.SaveChangesAsync(diagnostics.Token);
        }
        catch (Exception diagnosticsException)
        {
            logger.LogError(
                diagnosticsException,
                "No se pudo registrar el intento fallido del evento {ProgressEventId}.",
                progressEventId);
        }
    }

    private static string Truncate(string value) =>
        value.Length <= MaxLastErrorLength ? value : value[..MaxLastErrorLength];
}
