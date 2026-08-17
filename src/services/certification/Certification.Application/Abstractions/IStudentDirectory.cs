namespace Certification.Application.Abstractions;
public interface IStudentDirectory
{
    Task<StudentDirectoryEntry> GetDisplayNameAsync(
        Guid studentId,
        CancellationToken cancellationToken);
}