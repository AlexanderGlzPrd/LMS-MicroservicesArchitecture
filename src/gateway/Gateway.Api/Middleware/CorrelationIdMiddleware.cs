using BuildingBlocks.Observability;

namespace Gateway.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[LmsHeaderNames.CorrelationId].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
            context.Request.Headers[LmsHeaderNames.CorrelationId] = correlationId;
        }

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[LmsHeaderNames.CorrelationId] = correlationId;
            return Task.CompletedTask;
        });

        return next(context);
    }
}
