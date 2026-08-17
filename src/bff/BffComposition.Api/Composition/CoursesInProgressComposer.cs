using System.Collections.Concurrent;
using BffComposition.Api.Clients.CourseAuthoring;
using BffComposition.Api.Clients.Learning;
using BffComposition.Api.Contracts;
using BffComposition.Api.Errors;
using Microsoft.Extensions.Options;
namespace BffComposition.Api.Composition;

public sealed class CoursesInProgressComposer(
    LearningProgressClient learningProgressClient,
    CourseAuthoringCourseClient courseAuthoringCourseClient,
    IOptions<CourseAuthoringOptions> courseAuthoringOptions)
{
    private const string EnrichmentUnavailableCode = "CourseEnrichmentUnavailable";
    private const string NotInCatalogCode = "CourseNotInCatalog";

    public async Task<CoursesInProgressResponse> ComposeAsync(
        string status,
        CancellationToken cancellationToken)
    {
        var progress = await learningProgressClient.GetProgressAsync(status, cancellationToken);

        if (progress.Status is StudentProgressStatus.Unavailable)
        {
            throw new LearningUnavailableException();
        }

        var courseIds = progress.Courses
            .Select(course => course.CourseId)
            .Distinct()
            .ToList();

        var enrichments = await EnrichAsync(courseIds, cancellationToken);

        var items = progress.Courses
            .Select(course => ToItem(course, enrichments[course.CourseId]))
            .ToList();

        var warnings = courseIds
            .Select(courseId => ToWarning(courseId, enrichments[courseId]))
            .OfType<CompositionWarningResponse>()
            .ToList();

        return new CoursesInProgressResponse(items, warnings.Count > 0, warnings);
    }

    private async Task<IReadOnlyDictionary<Guid, CourseEnrichment>> EnrichAsync(
        IReadOnlyList<Guid> courseIds,
        CancellationToken cancellationToken)
    {
        var enrichments = new ConcurrentDictionary<Guid, CourseEnrichment>();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = courseAuthoringOptions.Value.MaxEnrichmentConcurrency,
            CancellationToken = cancellationToken,
        };

        await Parallel.ForEachAsync(
            courseIds,
            parallelOptions,
            async (courseId, token) =>
            {
                enrichments[courseId] = await courseAuthoringCourseClient
                    .GetCourseAsync(courseId, token);
            });

        return enrichments;
    }

    private static CourseInProgressItemResponse ToItem(
        StudentCourseProgress course,
        CourseEnrichment enrichment) => new(
            course.CourseId,
            enrichment.Title,
            enrichment.LessonCount,
            course.Status,
            course.StartedAt,
            course.CompletedAt,
            course.CompletedLessonCount,
            course.Percentage);

    private static CompositionWarningResponse? ToWarning(
        Guid courseId,
        CourseEnrichment enrichment) => enrichment.Status switch
        {
            CourseEnrichmentStatus.Unavailable => new CompositionWarningResponse(
                courseId,
                EnrichmentUnavailableCode,
                $"No se pudo obtener del catalogo la informacion del curso {courseId}."),

            CourseEnrichmentStatus.NotInCatalog => new CompositionWarningResponse(
                courseId,
                NotInCatalogCode,
                $"No hay ningun curso publicado con el identificador {courseId}."),

            _ => null,
        };
}
