namespace Learning.Infrastructure.Projection;
internal sealed class CorruptProgressEventPayloadException(
    Guid progressEventId, Exception? innerException = null)
    : Exception(
        $"El payload del evento '{progressEventId}' no produce un evento utilizable.",
        innerException)
{
    public Guid ProgressEventId { get; } = progressEventId;
}
