using Learning.Application.Abstractions;
using Learning.Application.Abstractions.Exceptions;
using Learning.Domain.Progress;
namespace Learning.Application.Progress.RecognizeGrantedAccess;
public sealed class RecognizeGrantedAccessHandler(
    ICourseProgressRepository progresses,
    IUnitOfWork unitOfWork,
    IInbox inbox,
    TimeProvider timeProvider)
{
    private const int MaxAttempts = 2;
    public async Task HandleAsync(
        RecognizeGrantedAccessCommand command,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            if (await inbox.HasBeenProcessedAsync(command.MessageId, cancellationToken))
            {
                return;
            }

            try
            {
                await ApplyAsync(command, cancellationToken);

                return;
            }
            catch (DuplicateInboxMessageException)
            {
                return;
            }
            catch (ConcurrentCourseProgressException) when (attempt < MaxAttempts)
            {
            }
        }
    }

    private async Task ApplyAsync(
        RecognizeGrantedAccessCommand command,
        CancellationToken cancellationToken)
    {
        var progress = await progresses.FindAsync(
            command.StudentId, command.CourseId, cancellationToken);

        if (progress is null)
        {
            progresses.Add(CourseProgress.Start(
                command.StudentId, command.CourseId, command.OccurredAt));
        }

        inbox.Record(command.MessageId, command.MessageType, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}