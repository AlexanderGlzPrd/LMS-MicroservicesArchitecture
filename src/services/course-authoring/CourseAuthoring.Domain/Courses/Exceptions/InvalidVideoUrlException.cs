using CourseAuthoring.Domain.Abstractions;
namespace CourseAuthoring.Domain.Courses.Exceptions;

public sealed class InvalidVideoUrlException(string? videoUrl)
    : DomainException($"La URL de video '{videoUrl}' no es una URL absoluta http o https.")
{
    public string? VideoUrl { get; } = videoUrl;
}
