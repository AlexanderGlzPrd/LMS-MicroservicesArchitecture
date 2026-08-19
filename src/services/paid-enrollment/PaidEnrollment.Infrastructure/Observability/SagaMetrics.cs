using System.Diagnostics.Metrics;
using PaidEnrollment.Application.Abstractions;
using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Infrastructure.Observability;
internal sealed class SagaMetrics : ISagaMetrics, IDisposable
{
    private readonly Meter meter = new("paid-enrollment");
    private readonly Counter<long> transitions;
    private readonly Counter<long> compensations;

    public SagaMetrics()
    {
        transitions = meter.CreateCounter<long>(
            "lms.saga.transitions",
            unit: "{transition}",
            description: "Transiciones de la Saga de compra persistidas con exito.");

        compensations = meter.CreateCounter<long>(
            "lms.saga.compensations",
            unit: "{compensation}",
            description: "Compensaciones de la Saga de compra ejecutadas.");
    }

    public void RecordTransition(PurchaseStatus from, PurchaseStatus to, string result) =>
        transitions.Add(
            1,
            new KeyValuePair<string, object?>("from_state", from.ToString()),
            new KeyValuePair<string, object?>("to_state", to.ToString()),
            new KeyValuePair<string, object?>("result", result));

    public void RecordCompensation(string operation, string result) =>
        compensations.Add(
            1,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("result", result));

    public void Dispose() => meter.Dispose();
}
