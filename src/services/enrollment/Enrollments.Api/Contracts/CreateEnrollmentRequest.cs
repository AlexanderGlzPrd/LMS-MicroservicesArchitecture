using System.ComponentModel.DataAnnotations;
namespace Enrollments.Api.Contracts;
public sealed record CreateEnrollmentRequest
{
    [Required]
    public Guid? CourseId { get; init; }
}
