using Microsoft.EntityFrameworkCore;
using PaymentProviderSim.Worker.Persistence;
namespace PaymentProviderSim.Worker.Messaging;
internal sealed class InboxRecorder(PaymentsDbContext context)
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