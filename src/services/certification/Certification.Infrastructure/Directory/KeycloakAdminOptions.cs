namespace Certification.Infrastructure.Directory;
public sealed class KeycloakAdminOptions
{
    public const string SectionName = "Keycloak";

    public string AdminBaseUrl { get; set; } = string.Empty;

    public string Realm { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string TokenEndpoint { get; set; } = string.Empty;

    public int TotalTimeoutSeconds { get; set; } = 5;

    public int RetryAttempts { get; set; } = 2;

    public int RetryBaseDelayMilliseconds { get; set; } = 200;

    public double CircuitBreakerFailureRatio { get; set; } = 0.5;

    public int CircuitBreakerSamplingSeconds { get; set; } = 30;

    public int CircuitBreakerMinimumThroughput { get; set; } = 3;

    public int CircuitBreakerBreakSeconds { get; set; } = 15;
}
