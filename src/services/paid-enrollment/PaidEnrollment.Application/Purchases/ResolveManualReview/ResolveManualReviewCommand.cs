using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Application.Purchases.ResolveManualReview;
public sealed record ResolveManualReviewCommand(
    PurchaseId PurchaseId,
    ManualResolution Resolution,
    string Evidence);
