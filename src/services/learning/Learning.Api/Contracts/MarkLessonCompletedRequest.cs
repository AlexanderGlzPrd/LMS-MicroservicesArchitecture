using System.ComponentModel.DataAnnotations;
namespace Learning.Api.Contracts;

public sealed record MarkLessonCompletedRequest
{
    [Required]
    public Guid? LessonId { get; init; }
}