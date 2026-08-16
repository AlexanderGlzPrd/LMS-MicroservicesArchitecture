using Learning.Application.Abstractions;
using Learning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace Learning.Infrastructure.Messaging;
internal sealed class InboxRecorder(LearningDbContext context) : IInbox
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
