using Microsoft.EntityFrameworkCore;
using PaidEnrollment.Application.Abstractions;
using PaidEnrollment.Infrastructure.Persistence;
namespace PaidEnrollment.Infrastructure.Messaging;
internal sealed class InboxRecorder(PaidEnrollmentDbContext context) : IInbox
{
    public Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken cancellationToken) =>
        context.InboxMessages.AnyAsync(
            message => message.MessageId == messageId, cancellationToken);

    public void Record(Guid messageId, string messageType, DateTimeOffset processedAt) =>
        context.InboxMessages.Add(new InboxMessage
        {
            MessageId = messageId,
            MessageType = messageType,
            ProcessedAt = processedAt,
        });
}
