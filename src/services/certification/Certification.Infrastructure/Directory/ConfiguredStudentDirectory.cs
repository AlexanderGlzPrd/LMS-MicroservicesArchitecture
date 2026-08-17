using Certification.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace Certification.Infrastructure.Directory;
internal sealed class ConfiguredStudentDirectory(
    IOptionsMonitor<StudentDirectoryOptions> options,
    ILogger<ConfiguredStudentDirectory> logger) : IStudentDirectory
{
    public Task<StudentDirectoryEntry> GetDisplayNameAsync(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Dictionary<string, string> students;

        try
        {
            students = options.CurrentValue.Students;
        }
        catch (OptionsValidationException exception)
        {
            logger.LogError(exception, "No se pudo leer el directorio de estudiantes.");

            return Task.FromResult(StudentDirectoryEntry.Unavailable);
        }

        foreach (var (key, displayName) in students)
        {
            if (!Guid.TryParse(key, out var configuredId) || configuredId != studentId)
            {
                continue;
            }

            return Task.FromResult(
                string.IsNullOrWhiteSpace(displayName)
                    ? StudentDirectoryEntry.NotFound
                    : StudentDirectoryEntry.Resolved(displayName));
        }

        return Task.FromResult(StudentDirectoryEntry.NotFound);
    }
}
