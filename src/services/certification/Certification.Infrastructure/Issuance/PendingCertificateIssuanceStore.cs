using Certification.Application.Abstractions;
using Certification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace Certification.Infrastructure.Issuance;
internal sealed class PendingCertificateIssuanceStore(CertificationDbContext context)
    : IPendingCertificateIssuances
{
    public async Task<DateTimeOffset?> FindCompletedAtAsync(
        Guid studentId,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var pending = await context.PendingCertificateIssuances
            .AsNoTracking()
            .FirstOrDefaultAsync(
                issuance => issuance.StudentId == studentId && issuance.CourseId == courseId,
                cancellationToken);

        return pending?.CompletedAt;
    }

    public void Add(
        Guid studentId,
        Guid courseId,
        DateTimeOffset completedAt,
        DateTimeOffset createdAt) =>
        context.PendingCertificateIssuances.Add(new PendingCertificateIssuance
        {
            StudentId = studentId,
            CourseId = courseId,
            CompletedAt = completedAt,
            CreatedAt = createdAt,
        });
}
