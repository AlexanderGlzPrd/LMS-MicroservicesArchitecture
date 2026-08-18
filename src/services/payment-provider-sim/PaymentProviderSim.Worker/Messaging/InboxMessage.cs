namespace PaymentProviderSim.Worker.Messaging;
internal sealed class InboxMessage
{
    public required Guid MessageId { get; init; }

    public required string MessageType { get; init; }

    public required DateTimeOffset ProcessedAt { get; init; }
}