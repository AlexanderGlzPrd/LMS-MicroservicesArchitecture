using System.Text.Json.Serialization;
namespace Certification.Infrastructure.Directory;
internal sealed record KeycloakUserResponse(
    [property: JsonPropertyName("firstName")] string? FirstName,
    [property: JsonPropertyName("lastName")] string? LastName,
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("enabled")] bool? Enabled);