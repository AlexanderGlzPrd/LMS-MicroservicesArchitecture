using System.Diagnostics;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Observability;

public static class ConsumeCorrelationExtensions
{
    public static void UseLmsConsumeCorrelation(
        this IConsumePipeConfigurator configurator,
        IRegistrationContext context) =>
        configurator.UseConsumeFilter(typeof(LmsConsumeCorrelationFilter<>), context);
}

public sealed class LmsConsumeCorrelationFilter<T>(
    ILogger<LmsConsumeCorrelationFilter<T>> logger,
    LmsServiceName serviceName) : IFilter<ConsumeContext<T>>
    where T : class
{
    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var activity = Activity.Current;

        var scope = new Dictionary<string, object?>
        {
            ["TraceId"] = activity?.TraceId.ToString(),
            ["SpanId"] = activity?.SpanId.ToString(),
            ["ServiceName"] = serviceName.Value,
            ["MessageId"] = context.MessageId?.ToString(),
        };

        if (context.InitiatorId is { } causationId)
        {
            scope["CausationId"] = causationId.ToString();
        }

        using (logger.BeginScope(scope))
        {
            await next.Send(context);
        }
    }

    public void Probe(ProbeContext context) => context.CreateFilterScope("lms-correlation");
}
