using System.Diagnostics;

namespace BuildingBlocks.Observability;

public static class OutboxTraceContext
{
    public static string? Capture() => Activity.Current?.Id;

    public static bool TryRestore(string? traceContext, out ActivityContext context)
    {
        context = default;

        if (string.IsNullOrWhiteSpace(traceContext))
        {
            return false;
        }

        return ActivityContext.TryParse(traceContext, traceState: null, out context);
    }
}
