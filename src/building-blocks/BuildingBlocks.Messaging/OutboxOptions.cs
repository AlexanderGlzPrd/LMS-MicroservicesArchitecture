namespace BuildingBlocks.Messaging;
public sealed class OutboxOptions
{
    public const string SectionName = "Messaging:Outbox";

    public bool Enabled { get; set; } = true;

    public int PollingIntervalSeconds { get; set; } = 5;

    public int BatchSize { get; set; } = 20;

    public int PublishTimeoutSeconds { get; set; } = 5;
}