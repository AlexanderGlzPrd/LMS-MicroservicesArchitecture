namespace Certification.Infrastructure.Directory;
public sealed class StudentDirectoryOptions
{
    public const string SectionName = "Certification:StudentDirectory";
    public Dictionary<string, string> Students { get; set; } = [];
}