using System.Text.Json;
using System.Text.Json.Serialization;
namespace Enrollments.Infrastructure.Messaging;
internal static class OutboxSerialization
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = false,
    };
}