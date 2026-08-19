namespace PaidEnrollment.Application.Purchases.Workflow;
public sealed record PaymentEvidence(
    DateTimeOffset? AuthorizedAt = null,
    DateTimeOffset? CapturedAt = null,
    DateTimeOffset? VoidedAt = null,
    DateTimeOffset? RefundedAt = null)
{
    public static readonly PaymentEvidence None = new();
}
