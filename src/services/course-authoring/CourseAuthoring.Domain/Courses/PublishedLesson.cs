namespace CourseAuthoring.Domain.Courses;

public sealed class PublishedLesson
{
    private PublishedLesson()
    {
    }

    // Conserva el LessonId de origen: Learning identifica lecciones completadas por ese id (ADR-T12).
    internal static PublishedLesson From(Lesson lesson) => new()
    {
        Id = lesson.Id,
        CourseId = lesson.CourseId,
        Title = lesson.Title,
        Description = lesson.Description,
        VideoUrl = lesson.VideoUrl,
        Position = lesson.Position,
    };

    public LessonId Id { get; private set; }

    public CourseId CourseId { get; private set; }

    public string Title { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public string VideoUrl { get; private set; } = null!;

    public int Position { get; private set; }
}
