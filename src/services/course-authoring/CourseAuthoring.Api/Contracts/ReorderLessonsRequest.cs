using System.ComponentModel.DataAnnotations;
namespace CourseAuthoring.Api.Contracts;

public sealed record ReorderLessonsRequest
{
    [Required]
    public IReadOnlyList<Guid> LessonIds { get; init; } = null!;
}
