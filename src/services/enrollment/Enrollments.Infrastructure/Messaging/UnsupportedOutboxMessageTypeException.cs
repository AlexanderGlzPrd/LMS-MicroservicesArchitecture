namespace Enrollments.Infrastructure.Messaging;
internal sealed class UnsupportedOutboxMessageTypeException(Guid messageId, string messageType)
    : Exception($"El mensaje '{messageId}' declara un tipo no soportado: '{messageType}'.")
{
    public Guid MessageId { get; } = messageId;

    public string MessageType { get; } = messageType;
}