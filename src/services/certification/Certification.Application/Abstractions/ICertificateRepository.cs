using Certification.Domain.Certificates;
namespace Certification.Application.Abstractions;
public interface ICertificateRepository
{
    Task<Certificate?> FindByIdAsync(
        CertificateId certificateId,
        CancellationToken cancellationToken);

    Task<Certificate?> FindByCompletionAsync(
        Guid studentId,
        Guid courseId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Certificate>> ListByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken);

    void Add(Certificate certificate);
}