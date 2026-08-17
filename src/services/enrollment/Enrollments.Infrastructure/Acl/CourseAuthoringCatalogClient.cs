using System.Net;
using Enrollments.Application.Abstractions;
using Enrollments.Domain.Enrollments;
using Polly;
namespace Enrollments.Infrastructure.Acl;
internal sealed class CourseAuthoringCatalogClient(HttpClient httpClient) : ICourseAvailability
{
    public async Task<CourseAvailability> CheckAsync(
        CourseId courseId,
        CancellationToken cancellationToken)
    {
        try
        {
            //Con ResponseHeadersRead solo se espera la cabecera de respuesta
            using var response = await httpClient.GetAsync(
                $"api/v1/catalog/courses/{courseId.Value}",
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            return response.StatusCode switch
            {
                HttpStatusCode.OK => CourseAvailability.Available,
                HttpStatusCode.NotFound => CourseAvailability.NotAvailable,

                _ => CourseAvailability.Unknown,
            };
        }
        catch (ExecutionRejectedException)
        {
            return CourseAvailability.Unknown;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CourseAvailability.Unknown;
        }
        catch (HttpRequestException)
        {
            return CourseAvailability.Unknown;
        }
    }
}
