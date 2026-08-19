using System.Net;
using System.Net.Http.Headers;
using PaidEnrollment.Application.Abstractions;
using PaidEnrollment.Domain.Purchases;
using PaidEnrollment.Infrastructure.Identity;
using Polly;
namespace PaidEnrollment.Infrastructure.Acl;
internal sealed class EnrollmentAccessClient(
    HttpClient httpClient,
    ServiceTokenProvider tokenProvider) : IEnrollmentAccess
{
    public async Task<EnrollmentAccess> CheckAsync(
        StudentId studentId,
        CourseId courseId,
        CancellationToken cancellationToken)
    {
        var access = await CheckOnceAsync(studentId, courseId, cancellationToken);
        if (access is null)
        {
            tokenProvider.Invalidate();

            access = await CheckOnceAsync(studentId, courseId, cancellationToken);
        }

        return access ?? EnrollmentAccess.Unknown;
    }

    private async Task<EnrollmentAccess?> CheckOnceAsync(
        StudentId studentId,
        CourseId courseId,
        CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetTokenAsync(cancellationToken);

        if (token is null)
        {
            return EnrollmentAccess.Unknown;
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/v1/enrollments/access?studentId={studentId.Value}&courseId={courseId.Value}");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            return response.StatusCode switch
            {
                HttpStatusCode.OK => EnrollmentAccess.Enrolled,
                HttpStatusCode.NotFound => EnrollmentAccess.NotEnrolled,
                HttpStatusCode.Unauthorized => null,

                _ => EnrollmentAccess.Unknown,
            };
        }
        catch (ExecutionRejectedException)
        {
            return EnrollmentAccess.Unknown;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return EnrollmentAccess.Unknown;
        }
        catch (HttpRequestException)
        {
            return EnrollmentAccess.Unknown;
        }
    }
}