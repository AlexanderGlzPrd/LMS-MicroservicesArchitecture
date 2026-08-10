namespace CourseAuthoring.Domain.Courses;

public sealed class Course
{
    private Course()
    {
    }

    /// <summary>
    /// Unica via de creacion de un curso. Nace siempre en <see cref="CourseStatus.Draft"/>.
    /// </summary>
    /// <exception cref="InvalidCourseTitleException">Si el titulo esta vacio o son solo espacios.</exception>
    public static Course Create(
        CourseId id,
        InstructorId instructorId,
        string title,
        DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidCourseTitleException();
        }

        return new Course
        {
            Id = id,
            InstructorId = instructorId,
            Title = title,
            Status = CourseStatus.Draft,
            CreatedAt = createdAt,
        };
    }

    public CourseId Id { get; private set; }

    public InstructorId InstructorId { get; private set; }

    public string Title { get; private set; } = null!;

    public CourseStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}
