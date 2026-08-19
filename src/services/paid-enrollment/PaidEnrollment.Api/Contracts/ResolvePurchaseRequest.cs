using System.ComponentModel.DataAnnotations;
using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Api.Contracts;
public sealed record ResolvePurchaseRequest
{
    [Required]
    public string Resolution { get; init; } = null!;

    [Required]
    [StringLength(PurchaseResolution.MaxEvidenceLength, MinimumLength = 1)]
    public string Evidence { get; init; } = null!;
}