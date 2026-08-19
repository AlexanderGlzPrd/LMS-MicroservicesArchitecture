namespace PaidEnrollment.Application.Purchases.Workflow;
public enum ReplyReaction
{
    CorrelationMismatch = 1,
    AlreadyProcessed = 2,
    Applied = 3,

    NotApplicable = 4,
    Late = 5,
    EvidenceOnly = 6,
}
