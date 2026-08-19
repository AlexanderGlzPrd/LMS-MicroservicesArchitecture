using System.ComponentModel.DataAnnotations;
namespace PaidEnrollment.Api.Contracts;
public sealed record StartPurchaseRequest
{
    [Required]
    public Guid CourseId { get; init; }
}