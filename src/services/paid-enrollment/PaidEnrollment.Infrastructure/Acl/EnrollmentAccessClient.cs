using System.Net;
using PaidEnrollment.Application.Abstractions;
using PaidEnrollment.Domain.Purchases;
using Polly;
namespace PaidEnrollment.Infrastructure.Acl;
internal sealed class EnrollmentAccessClient(HttpClient httpClient) : IEnrollmentAccess
{
    private const string StudentHeaderName = "X-Student-Id";

    public async Task<EnrollmentAccess> CheckAsync(
        StudentId studentId,
        CourseId courseId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get, $"api/v1/me/enrollments/{courseId.Value}");

            request.Headers.Add(StudentHeaderName, studentId.Value.ToString());

            using var response = await httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            return response.StatusCode switch
            {
                HttpStatusCode.OK => EnrollmentAccess.Enrolled,
                HttpStatusCode.NotFound => EnrollmentAccess.NotEnrolled,

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
