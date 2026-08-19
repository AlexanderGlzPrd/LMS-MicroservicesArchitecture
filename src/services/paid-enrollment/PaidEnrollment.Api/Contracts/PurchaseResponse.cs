using PaidEnrollment.Application.Purchases;
namespace PaidEnrollment.Api.Contracts;
public sealed record PurchaseResponse(
    Guid PurchaseId,
    Guid CourseId,
    Guid? PaymentId,
    decimal Amount,
    string Currency,
    string Status,
    string? Reason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static PurchaseResponse From(PurchaseView view) => new(
        view.PurchaseId,
        view.CourseId,
        PaymentId: null,
        view.Amount,
        view.Currency,
        view.Status,
        view.Reason,
        view.CreatedAt,
        view.UpdatedAt);

    public static PurchaseResponse Detailed(PurchaseView view) => new(
        view.PurchaseId,
        view.CourseId,
        view.PaymentId,
        view.Amount,
        view.Currency,
        view.Status,
        view.Reason,
        view.CreatedAt,
        view.UpdatedAt);
}