using Certification.Application.Abstractions;
using Certification.Domain.Certificates;
using Microsoft.EntityFrameworkCore;
namespace Certification.Infrastructure.Persistence;
internal sealed class CertificateRepository(CertificationDbContext context) : ICertificateRepository
{
    public Task<Certificate?> FindByIdAsync(
        CertificateId certificateId,
        CancellationToken cancellationToken) =>
        context.Certificates
            .AsNoTracking()
            .FirstOrDefaultAsync(
                certificate => certificate.CertificateId == certificateId,
                cancellationToken);

    public Task<Certificate?> FindByCompletionAsync(
        Guid studentId,
        Guid courseId,
        CancellationToken cancellationToken) =>
        context.Certificates
            .AsNoTracking()
            .FirstOrDefaultAsync(
                certificate => certificate.StudentId == studentId
                    && certificate.CourseId == courseId,
                cancellationToken);

    public async Task<IReadOnlyList<Certificate>> ListByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken) =>
        await context.Certificates
            .AsNoTracking()
            .Where(certificate => certificate.StudentId == studentId)
            .OrderByDescending(certificate => certificate.IssuedAt)
            .ThenBy(certificate => certificate.CourseId)
            .ToListAsync(cancellationToken);

    public void Add(Certificate certificate) => context.Certificates.Add(certificate);
}
