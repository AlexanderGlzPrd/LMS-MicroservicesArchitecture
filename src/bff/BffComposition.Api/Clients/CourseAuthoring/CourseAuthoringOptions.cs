namespace BffComposition.Api.Clients.CourseAuthoring;
public sealed class CourseAuthoringOptions : HttpResilienceSettings
{
    public const string SectionName = "Services:CourseAuthoring";

    public int MaxEnrichmentConcurrency { get; set; } = 8;
}