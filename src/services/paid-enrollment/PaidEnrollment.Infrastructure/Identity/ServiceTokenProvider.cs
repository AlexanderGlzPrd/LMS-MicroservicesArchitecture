using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaidEnrollment.Infrastructure.Acl;
using Polly;
namespace PaidEnrollment.Infrastructure.Identity;
internal sealed class ServiceTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<EnrollmentOptions> options,
    TimeProvider timeProvider,
    ILogger<ServiceTokenProvider> logger)
{
    public const string HttpClientName = "enrollment-token";

    private const int ExpiryMarginSeconds = 30;

    private readonly SemaphoreSlim gate = new(1, 1);

    private string? cachedToken;
    private DateTimeOffset expiresAt = DateTimeOffset.MinValue;

    public async Task<string?> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (TryGetCachedToken(out var cached))
        {
            return cached;
        }

        await gate.WaitAsync(cancellationToken);

        try
        {
            // Otra tarea pudo renovarlo mientras esta esperaba
            if (TryGetCachedToken(out var renewed))
            {
                return renewed;
            }

            return await RequestTokenAsync(cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public void Invalidate()
    {
        cachedToken = null;
        expiresAt = DateTimeOffset.MinValue;
    }

    private bool TryGetCachedToken(out string? token)
    {
        token = cachedToken;

        return token is { Length: > 0 } && timeProvider.GetUtcNow() < expiresAt;
    }

    private async Task<string?> RequestTokenAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;

        using var request = new HttpRequestMessage(HttpMethod.Post, settings.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = settings.ClientId,
                ["client_secret"] = settings.ClientSecret,
            }),
        };

        try
        {
            using var httpClient = httpClientFactory.CreateClient(HttpClientName);
            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Keycloak rechazo la peticion de token de servicio con {StatusCode}.",
                    (int)response.StatusCode);

                return null;
            }

            var body = await response.Content.ReadFromJsonAsync<ServiceTokenResponse>(
                cancellationToken);

            if (body?.AccessToken is not { Length: > 0 } accessToken)
            {
                logger.LogWarning("La respuesta de token de servicio no traia access_token.");

                return null;
            }

            cachedToken = accessToken;
            expiresAt = timeProvider.GetUtcNow()
                .AddSeconds(Math.Max(body.ExpiresIn - ExpiryMarginSeconds, 0));

            return accessToken;
        }
        catch (JsonException)
        {
            return TokenUnavailable();
        }
        catch (ExecutionRejectedException)
        {
            return TokenUnavailable();
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return TokenUnavailable();
        }
        catch (HttpRequestException)
        {
            return TokenUnavailable();
        }
    }

    private string? TokenUnavailable()
    {
        logger.LogWarning("No se pudo obtener el token de servicio contra Keycloak.");

        return null;
    }

    private sealed record ServiceTokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}