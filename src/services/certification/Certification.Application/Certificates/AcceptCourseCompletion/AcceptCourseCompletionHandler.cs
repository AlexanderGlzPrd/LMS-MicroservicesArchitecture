using Certification.Application.Abstractions;
using Certification.Application.Abstractions.Exceptions;
namespace Certification.Application.Certificates.AcceptCourseCompletion;
public sealed class AcceptCourseCompletionHandler(
    IInbox inbox,
    ICertificateRepository certificates,
    IPendingCertificateIssuances pendingIssuances,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private const int MaxAttempts = 2;

    public async Task HandleAsync(
        AcceptCourseCompletionCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            if (await inbox.HasBeenProcessedAsync(command.MessageId, cancellationToken))
            {
                return;
            }

            var registered = await FindRegisteredCompletionAsync(command, cancellationToken);

            if (registered is { } registeredCompletedAt)
            {
                if (registeredCompletedAt != command.CompletedAt)
                {
                    throw new ContradictoryCourseCompletionException(
                        command.StudentId,
                        command.CourseId,
                        registeredCompletedAt,
                        command.CompletedAt);
                }

                inbox.Record(command.MessageId, command.MessageType, timeProvider.GetUtcNow());

                try
                {
                    await unitOfWork.SaveChangesAsync(cancellationToken);

                    return;
                }
                catch (DuplicateInboxMessageException)
                {
                    return;
                }
            }

            var now = timeProvider.GetUtcNow();

            pendingIssuances.Add(command.StudentId, command.CourseId, command.CompletedAt, now);
            inbox.Record(command.MessageId, command.MessageType, now);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);

                return;
            }
            catch (DuplicateInboxMessageException)
            {
                return;
            }
            catch (DuplicatePendingIssuanceException) when (attempt < MaxAttempts)
            {
            }
        }
    }

    private async Task<DateTimeOffset?> FindRegisteredCompletionAsync(
        AcceptCourseCompletionCommand command,
        CancellationToken cancellationToken)
    {
        var certificate = await certificates.FindByCompletionAsync(
            command.StudentId, command.CourseId, cancellationToken);

        if (certificate is not null)
        {
            return certificate.CompletedAt;
        }

        return await pendingIssuances.FindCompletedAtAsync(
            command.StudentId, command.CourseId, cancellationToken);
    }
}
