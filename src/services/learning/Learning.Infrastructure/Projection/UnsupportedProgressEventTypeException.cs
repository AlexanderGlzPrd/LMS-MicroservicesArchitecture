namespace Learning.Infrastructure.Projection;
internal sealed class UnsupportedProgressEventTypeException(Guid progressEventId, string eventType)
    : Exception($"El evento '{progressEventId}' declara un tipo no soportado: '{eventType}'.")
{
    public Guid ProgressEventId { get; } = progressEventId;

    public string EventType { get; } = eventType;
}
