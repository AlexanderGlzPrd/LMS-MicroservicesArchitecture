namespace BffComposition.Api.Clients.Learning;
public sealed class StudentProgressLookup
{
    private StudentProgressLookup(
        StudentProgressStatus status,
        IReadOnlyList<StudentCourseProgress> courses)
    {
        Status = status;
        Courses = courses;
    }

    public StudentProgressStatus Status { get; }

    public IReadOnlyList<StudentCourseProgress> Courses { get; }

    public static StudentProgressLookup Unavailable { get; } =
        new(StudentProgressStatus.Unavailable, []);

    public static StudentProgressLookup Available(IReadOnlyList<StudentCourseProgress> courses) =>
        new(StudentProgressStatus.Available, courses);
}

public sealed record StudentCourseProgress(
    Guid CourseId,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    int CompletedLessonCount,
    decimal? Percentage);
