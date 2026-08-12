using System.ComponentModel.DataAnnotations;
namespace CourseAuthoring.Api.Contracts;
public sealed record CreateLessonRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = null!;

    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Description { get; init; } = null!;

    [Required]
    [StringLength(2048, MinimumLength = 1)]
    [Url]
    public string VideoUrl { get; init; } = null!;
}
