using System.ComponentModel.DataAnnotations;
namespace CourseAuthoring.Api.Contracts;
public sealed record CreateCourseRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = null!;
}