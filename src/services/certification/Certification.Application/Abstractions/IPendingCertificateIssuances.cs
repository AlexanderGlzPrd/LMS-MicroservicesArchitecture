namespace Certification.Application.Abstractions;
public interface IPendingCertificateIssuances
{
    Task<DateTimeOffset?> FindCompletedAtAsync(
        Guid studentId,
        Guid courseId,
        CancellationToken cancellationToken);

    void Add(Guid studentId, Guid courseId, DateTimeOffset completedAt, DateTimeOffset createdAt);
}