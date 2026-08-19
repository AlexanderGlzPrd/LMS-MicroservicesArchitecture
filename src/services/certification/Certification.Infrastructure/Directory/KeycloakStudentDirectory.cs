using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Certification.Application.Abstractions;
using Certification.Infrastructure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
namespace Certification.Infrastructure.Directory;
internal sealed class KeycloakStudentDirectory(
    HttpClient httpClient,
    ServiceTokenProvider tokenProvider,
    IOptions<KeycloakAdminOptions> options,
    ILogger<KeycloakStudentDirectory> logger) : IStudentDirectory
{
    public async Task<StudentDirectoryEntry> GetDisplayNameAsync(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var entry = await LookupAsync(studentId, cancellationToken);

        if (entry is null)
        {
            tokenProvider.Invalidate();

            entry = await LookupAsync(studentId, cancellationToken);
        }

        return entry ?? StudentDirectoryEntry.Unavailable;
    }

    private async Task<StudentDirectoryEntry?> LookupAsync(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetTokenAsync(cancellationToken);

        if (token is null)
        {
            return StudentDirectoryEntry.Unavailable;
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"admin/realms/{options.Value.Realm}/users/{studentId}");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode is HttpStatusCode.Unauthorized)
            {
                return null;
            }

            if (response.StatusCode is HttpStatusCode.Forbidden)
            {
                logger.LogError(
                    "Keycloak rechazo la consulta del estudiante {StudentId} con 403: "
                    + "revisar el rol view-users de la cuenta de servicio.",
                    studentId);

                return StudentDirectoryEntry.Unavailable;
            }

            if (response.StatusCode is HttpStatusCode.NotFound)
            {
                logger.LogWarning(
                    "El estudiante {StudentId} no existe en el realm.",
                    studentId);

                return StudentDirectoryEntry.NotFound;
            }

            if (response.StatusCode is not HttpStatusCode.OK)
            {
                return StudentDirectoryEntry.Unavailable;
            }

            var body = await response.Content.ReadFromJsonAsync<KeycloakUserResponse>(
                cancellationToken);

            return Translate(studentId, body);
        }
        catch (JsonException)
        {
            return StudentDirectoryEntry.Unavailable;
        }
        catch (ExecutionRejectedException)
        {
            return StudentDirectoryEntry.Unavailable;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return StudentDirectoryEntry.Unavailable;
        }
        catch (HttpRequestException)
        {
            return StudentDirectoryEntry.Unavailable;
        }
    }

    private StudentDirectoryEntry Translate(Guid studentId, KeycloakUserResponse? body)
    {
        if (body is null)
        {
            return StudentDirectoryEntry.Unavailable;
        }

        var displayName = $"{body.FirstName} {body.LastName}".Trim();

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = body.Username?.Trim() ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            logger.LogWarning(
                "El estudiante {StudentId} existe en el realm pero no tiene nombre visible.",
                studentId);

            return StudentDirectoryEntry.NotFound;
        }

        return StudentDirectoryEntry.Resolved(displayName);
    }
}