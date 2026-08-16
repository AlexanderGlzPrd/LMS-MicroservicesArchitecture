namespace Learning.Infrastructure.Projection;
public sealed class ProjectionOptions
{
    public const string SectionName = "Projection";

    public bool Enabled { get; set; } = true;

    public int PollingIntervalSeconds { get; set; } = 5;

    public int BatchSize { get; set; } = 50;

    public int DiagnosticsTimeoutSeconds { get; set; } = 5;
}
