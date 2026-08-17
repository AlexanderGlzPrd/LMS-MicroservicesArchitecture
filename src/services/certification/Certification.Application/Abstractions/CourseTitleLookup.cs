namespace Certification.Application.Abstractions;
public sealed class CourseTitleLookup
{
    private CourseTitleLookup(CourseTitleStatus status, string title)
    {
        Status = status;
        Title = title;
    }

    public static CourseTitleLookup Resolved(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Un titulo resuelto no puede estar vacio ni en blanco.",
                nameof(title));
        }

        return new CourseTitleLookup(CourseTitleStatus.Resolved, title);
    }

    public static readonly CourseTitleLookup NotFound =
        new(CourseTitleStatus.NotFound, string.Empty);

    public static readonly CourseTitleLookup Unavailable =
        new(CourseTitleStatus.Unavailable, string.Empty);

    public CourseTitleStatus Status { get; }

    public string Title { get; }
}