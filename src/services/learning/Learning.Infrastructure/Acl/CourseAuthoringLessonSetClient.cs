using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Learning.Application.Abstractions;
using Learning.Domain.Progress;
using Polly;
namespace Learning.Infrastructure.Acl;
internal sealed class CourseAuthoringLessonSetClient(HttpClient httpClient) : ICurrentLessonSet
{
    public async Task<CurrentLessonSet> GetAsync(CourseId courseId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(
                $"api/v1/catalog/courses/{courseId.Value}/lesson-ids",
                cancellationToken);

            if (response.StatusCode is HttpStatusCode.NotFound)
            {
                return CurrentLessonSet.NotAvailable;
            }

            if (response.StatusCode is not HttpStatusCode.OK)
            {
                return CurrentLessonSet.Unknown;
            }

            var body = await response.Content.ReadFromJsonAsync<LessonIdsResponse>(cancellationToken);

            return Translate(courseId, body);
        }
        catch (JsonException)
        {
            return CurrentLessonSet.Unknown;
        }
        catch (ExecutionRejectedException)
        {
            return CurrentLessonSet.Unknown;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CurrentLessonSet.Unknown;
        }
        catch (HttpRequestException)
        {
            return CurrentLessonSet.Unknown;
        }
    }

    private static CurrentLessonSet Translate(CourseId courseId, LessonIdsResponse? body)
    {
        if (body?.CourseId is not { } respondedCourseId || body.LessonIds is not { } lessonIds)
        {
            return CurrentLessonSet.Unknown;
        }

        if (respondedCourseId == Guid.Empty || respondedCourseId != courseId.Value)
        {
            return CurrentLessonSet.Unknown;
        }

        if (lessonIds.Count == 0 || lessonIds.Contains(Guid.Empty))
        {
            return CurrentLessonSet.Unknown;
        }

        var distinct = new HashSet<LessonId>(lessonIds.Select(lessonId => new LessonId(lessonId)));

        if (distinct.Count != lessonIds.Count)
        {
            return CurrentLessonSet.Unknown;
        }

        return CurrentLessonSet.Available(distinct);
    }
}
