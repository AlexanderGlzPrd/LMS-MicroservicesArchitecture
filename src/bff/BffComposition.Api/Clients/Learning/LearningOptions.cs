namespace BffComposition.Api.Clients.Learning;
public sealed class LearningOptions : HttpResilienceSettings
{
    public const string SectionName = "Services:Learning";

    public int RetryAfterSeconds { get; set; } = 5;
}