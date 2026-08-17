namespace Certification.Infrastructure.Acl;
public sealed class CourseAuthoringOptions
{
    public const string SectionName = "Services:CourseAuthoring";

    public string BaseUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 3;

    public int TotalTimeoutSeconds { get; set; } = 5;

    public int RetryAttempts { get; set; } = 2;

    public int RetryBaseDelayMilliseconds { get; set; } = 200;

    public double CircuitBreakerFailureRatio { get; set; } = 0.5;

    public int CircuitBreakerSamplingSeconds { get; set; } = 30;

    public int CircuitBreakerMinimumThroughput { get; set; } = 3;

    public int CircuitBreakerBreakSeconds { get; set; } = 15;
}
