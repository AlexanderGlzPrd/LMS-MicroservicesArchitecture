namespace Certification.Application.Abstractions;
public sealed class StudentDirectoryEntry
{
    private StudentDirectoryEntry(StudentDirectoryStatus status, string displayName)
    {
        Status = status;
        DisplayName = displayName;
    }

    public static StudentDirectoryEntry Resolved(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException(
                "Un nombre resuelto no puede estar vacio ni en blanco.",
                nameof(displayName));
        }

        return new StudentDirectoryEntry(StudentDirectoryStatus.Resolved, displayName);
    }

    public static readonly StudentDirectoryEntry NotFound =
        new(StudentDirectoryStatus.NotFound, string.Empty);

    public static readonly StudentDirectoryEntry Unavailable =
        new(StudentDirectoryStatus.Unavailable, string.Empty);

    public StudentDirectoryStatus Status { get; }

    public string DisplayName { get; }
}