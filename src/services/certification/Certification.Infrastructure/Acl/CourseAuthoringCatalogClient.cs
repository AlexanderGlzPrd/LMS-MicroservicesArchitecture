using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Certification.Application.Abstractions;
namespace Certification.Infrastructure.Acl;
internal sealed class CourseAuthoringCatalogClient(HttpClient httpClient) : ICourseCatalog
{
    public async Task<CourseTitleLookup> GetTitleAsync(
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
                return CourseTitleLookup.NotFound;
            }

            if (response.StatusCode is not HttpStatusCode.OK)
            {
                return CourseTitleLookup.Unavailable;
            }

            var body = await response.Content.ReadFromJsonAsync<CatalogCourseResponse>(
                cancellationToken);

            return Translate(courseId, body);
        }
        catch (JsonException)
        {
            return CourseTitleLookup.Unavailable;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CourseTitleLookup.Unavailable;
        }
        catch (HttpRequestException)
        {
            return CourseTitleLookup.Unavailable;
        }
    }

    private static CourseTitleLookup Translate(Guid courseId, CatalogCourseResponse? body)
    {
        if (body?.Id is not { } respondedCourseId || body.Title is not { } title)
        {
            return CourseTitleLookup.Unavailable;
        }

        if (respondedCourseId == Guid.Empty || respondedCourseId != courseId)
        {
            return CourseTitleLookup.Unavailable;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return CourseTitleLookup.Unavailable;
        }

        return CourseTitleLookup.Resolved(title);
    }
}
