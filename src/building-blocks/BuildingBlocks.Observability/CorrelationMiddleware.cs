using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Observability;

public static class CorrelationApplicationBuilderExtensions
{
    public static IApplicationBuilder UseLmsCorrelation(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationMiddleware>();
}

internal sealed class CorrelationMiddleware(
    RequestDelegate next,
    ILogger<CorrelationMiddleware> logger,
    LmsServiceName serviceName)
{
    internal const string ItemKey = "lms.correlation-id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[LmsHeaderNames.CorrelationId].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
            context.Request.Headers[LmsHeaderNames.CorrelationId] = correlationId;
        }

        context.Items[ItemKey] = correlationId;

        var activity = Activity.Current;

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["TraceId"] = activity?.TraceId.ToString(),
            ["SpanId"] = activity?.SpanId.ToString(),
            ["CorrelationId"] = correlationId,
            ["ServiceName"] = serviceName.Value,
        }))
        {
            await next(context);
        }
    }
}

internal sealed class CorrelationPropagationHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains(LmsHeaderNames.CorrelationId)
            && accessor.HttpContext?.Items.TryGetValue(CorrelationMiddleware.ItemKey, out var value) == true
            && value is string correlationId
            && !string.IsNullOrWhiteSpace(correlationId))
        {
            request.Headers.TryAddWithoutValidation(LmsHeaderNames.CorrelationId, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
