using System.Text.Json;
using Learning.Application.Abstractions;
using Learning.Application.Abstractions.Exceptions;
using Learning.Contracts.V1;
using Learning.Domain.Abstractions;
using Learning.Domain.Progress;
using Learning.Domain.Progress.Events;
using Learning.Infrastructure.Messaging;
using Learning.Infrastructure.Persistence.Configurations;
using Learning.Infrastructure.Projection;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace Learning.Infrastructure.Persistence;
internal sealed class UnitOfWork(LearningDbContext context) : IUnitOfWork
{
    private readonly OutboxWriter _outboxWriter = new(context);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        TranslateDomainEvents();

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsKnownRace(exception))
        {
            context.ChangeTracker.Clear();
            throw new ConcurrentCourseProgressException(exception);
        }
        catch (DbUpdateException exception) when (IsDuplicateInboxMessage(exception))
        {
            context.ChangeTracker.Clear();
            throw new DuplicateInboxMessageException(exception);
        }
    }

    // Unico punto del servicio donde un evento de dominio se convierte en fila.
    private void TranslateDomainEvents()
    {
        var entries = context.ChangeTracker.Entries<CourseProgress>().ToList();

        foreach (var entry in entries)
        {
            var progress = entry.Entity;

            if (progress.DomainEvents.Count == 0)
            {
                continue;
            }

            var domainEvents = progress.DomainEvents.ToList();
            progress.ClearDomainEvents();

            foreach (var domainEvent in domainEvents)
            {
                context.ProgressEvents.Add(ToRow(progress, domainEvent));

                if (domainEvent is CourseProgressCompleted completed)
                {
                    _outboxWriter.Enqueue(ToContract(progress, completed));
                }
            }
        }
    }

    private static ProgressEvent ToRow(CourseProgress progress, IDomainEvent domainEvent) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            StudentId = progress.StudentId.Value,
            CourseId = progress.CourseId.Value,
            EventType = domainEvent.GetType().FullName!,
            Payload = JsonSerializer.Serialize(
                domainEvent, domainEvent.GetType(), ProgressEventSerialization.Options),
            OccurredAt = domainEvent.OccurredAt,
        };

    private static CourseCompleted ToContract(
        CourseProgress progress, CourseProgressCompleted completed) =>
        new()
        {
            StudentId = progress.StudentId.Value,
            CourseId = progress.CourseId.Value,
            CompletedAt = completed.OccurredAt,
        };

    private static bool IsKnownRace(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: CourseProgressConfiguration.PrimaryKeyName
                or CompletedLessonConfiguration.PrimaryKeyName,
        };

    private static bool IsDuplicateInboxMessage(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: InboxMessageConfiguration.PrimaryKeyName,
        };
}
