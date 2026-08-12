using CourseAuthoring.Domain.Courses.Exceptions;
namespace CourseAuthoring.Domain.Courses;

public sealed class Lesson
{
    private Lesson()
    {
    }

    internal static Lesson Create(
        LessonId id,
        CourseId courseId,
        string title,
        string description,
        string videoUrl,
        int position)
    {
        var lesson = new Lesson
        {
            Id = id,
            CourseId = courseId,
            Position = position,
        };

        lesson.Apply(title, description, videoUrl);

        return lesson;
    }

    internal void Update(string title, string description, string videoUrl)
        => Apply(title, description, videoUrl);

    internal void MoveTo(int position) => Position = position;


    private void Apply(string title, string description, string videoUrl)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidLessonTitleException();
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new InvalidLessonDescriptionException();
        }

        if (!IsAbsoluteHttpUrl(videoUrl))
        {
            throw new InvalidVideoUrlException(videoUrl);
        }

        Title = title;
        Description = description;
        VideoUrl = videoUrl;
    }

    private static bool IsAbsoluteHttpUrl(string videoUrl)
        => Uri.TryCreate(videoUrl, UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    public LessonId Id { get; private set; }

    public CourseId CourseId { get; private set; }

    public string Title { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public string VideoUrl { get; private set; } = null!;

    public int Position { get; private set; }
}
