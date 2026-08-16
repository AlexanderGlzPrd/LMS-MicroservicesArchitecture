namespace Enrollments.Infrastructure.Messaging;
internal sealed class CorruptOutboxPayloadException(Guid messageId, Exception? innerException = null)
    : Exception($"El payload del mensaje '{messageId}' no produce un contrato utilizable.", innerException)
{
    public Guid MessageId { get; } = messageId;
}