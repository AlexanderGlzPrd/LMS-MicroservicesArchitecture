namespace BffComposition.Api.Clients.CourseAuthoring;
internal sealed class CourseEnrichment
{
    private CourseEnrichment(CourseEnrichmentStatus status, string? title, int? lessonCount)
    {
        Status = status;
        Title = title;
        LessonCount = lessonCount;
    }

    public CourseEnrichmentStatus Status { get; }

    public string? Title { get; }

    public int? LessonCount { get; }

    public static CourseEnrichment Unavailable { get; } =
        new(CourseEnrichmentStatus.Unavailable, null, null);

    public static CourseEnrichment NotInCatalog { get; } =
        new(CourseEnrichmentStatus.NotInCatalog, null, null);

    public static CourseEnrichment Resolved(string title, int lessonCount) =>
        new(CourseEnrichmentStatus.Resolved, title, lessonCount);
}