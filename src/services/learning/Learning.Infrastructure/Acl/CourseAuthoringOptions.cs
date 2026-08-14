namespace Learning.Infrastructure.Acl;

public sealed class CourseAuthoringOptions
{
    public const string SectionName = "Services:CourseAuthoring";

    public string BaseUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 3;

    public int RetryAfterSeconds { get; set; } = 5;
}
