using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Net.Http.Headers;
using Polly;
namespace BffComposition.Api.Clients.Learning;
public sealed class LearningProgressClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor)
{
    public async Task<StudentProgressLookup> GetProgressAsync(
        string status,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/v1/me/course-progress?status={status}");

            // Se propaga el token del usuario, no una identidad de servicio
            var authorization = httpContextAccessor.HttpContext?
                .Request.Headers[HeaderNames.Authorization].ToString();

            if (!string.IsNullOrWhiteSpace(authorization))
            {
                request.Headers.TryAddWithoutValidation(HeaderNames.Authorization, authorization);
            }

            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode is not HttpStatusCode.OK)
            {
                return StudentProgressLookup.Unavailable;
            }

            var body = await response.Content
                .ReadFromJsonAsync<IReadOnlyList<CourseProgressResponse>>(cancellationToken);

            return Translate(body);
        }
        catch (JsonException)
        {
            return StudentProgressLookup.Unavailable;
        }
        catch (ExecutionRejectedException)
        {
            return StudentProgressLookup.Unavailable;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return StudentProgressLookup.Unavailable;
        }
        catch (HttpRequestException)
        {
            return StudentProgressLookup.Unavailable;
        }
    }

    private static StudentProgressLookup Translate(IReadOnlyList<CourseProgressResponse>? body)
    {
        if (body is null)
        {
            return StudentProgressLookup.Unavailable;
        }

        var courses = new List<StudentCourseProgress>(body.Count);

        foreach (var item in body)
        {
            if (item.CourseId is not { } courseId
                || courseId == Guid.Empty
                || item.Status is not { } status
                || string.IsNullOrWhiteSpace(status)
                || item.StartedAt is not { } startedAt
                || item.CompletedLessonCount is not { } completedLessonCount)
            {
                return StudentProgressLookup.Unavailable;
            }

            courses.Add(new StudentCourseProgress(
                courseId,
                status,
                startedAt,
                item.CompletedAt,
                completedLessonCount,
                item.Percentage));
        }

        return StudentProgressLookup.Available(courses);
    }
}