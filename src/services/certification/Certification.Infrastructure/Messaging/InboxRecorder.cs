using Certification.Application.Abstractions;
using Certification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace Certification.Infrastructure.Messaging;
internal sealed class InboxRecorder(CertificationDbContext context) : IInbox
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
