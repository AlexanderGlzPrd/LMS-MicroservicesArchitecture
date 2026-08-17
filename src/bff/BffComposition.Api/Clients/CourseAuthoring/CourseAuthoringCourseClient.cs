using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Polly;
namespace BffComposition.Api.Clients.CourseAuthoring;
internal sealed class CourseAuthoringCourseClient(HttpClient httpClient)
{
    public async Task<CourseEnrichment> GetCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(
                $"api/v1/catalog/courses/{courseId}",
                cancellationToken);

            if (response.StatusCode is HttpStatusCode.NotFound)
            {
                return CourseEnrichment.NotInCatalog;
            }

            if (response.StatusCode is not HttpStatusCode.OK)
            {
                return CourseEnrichment.Unavailable;
            }

            var body = await response.Content
                .ReadFromJsonAsync<CatalogCourseResponse>(cancellationToken);

            return Translate(courseId, body);
        }
        catch (JsonException)
        {
            return CourseEnrichment.Unavailable;
        }
        catch (ExecutionRejectedException)
        {
            return CourseEnrichment.Unavailable;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CourseEnrichment.Unavailable;
        }
        catch (HttpRequestException)
        {
            return CourseEnrichment.Unavailable;
        }
    }

    private static CourseEnrichment Translate(Guid courseId, CatalogCourseResponse? body)
    {
        if (body?.Id is not { } respondedCourseId || body.Title is not { } title)
        {
            return CourseEnrichment.Unavailable;
        }

        if (respondedCourseId == Guid.Empty || respondedCourseId != courseId)
        {
            return CourseEnrichment.Unavailable;
        }

        if (string.IsNullOrWhiteSpace(title) || body.Lessons is not { } lessons)
        {
            return CourseEnrichment.Unavailable;
        }

        return CourseEnrichment.Resolved(title, lessons.Count);
    }
}
