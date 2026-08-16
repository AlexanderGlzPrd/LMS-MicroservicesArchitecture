using System.Text.Json;
using System.Text.Json.Serialization;
namespace Learning.Infrastructure.Projection;
internal static class ProgressEventSerialization
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = false,
    };
}
