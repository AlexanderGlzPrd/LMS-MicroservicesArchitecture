using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Application.Purchases;
public sealed record PurchaseView(
    Guid PurchaseId,
    Guid CourseId,
    Guid PaymentId,
    decimal Amount,
    string Currency,
    string Status,
    string? Reason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static PurchaseView From(Purchase purchase) => new(
        purchase.Id.Value,
        purchase.CourseId.Value,
        purchase.PaymentId.Value,
        purchase.Price.Amount,
        purchase.Price.Currency,
        purchase.Status.ToString(),
        purchase.Reason?.ToString(),
        purchase.CreatedAt,
        purchase.UpdatedAt);
}
