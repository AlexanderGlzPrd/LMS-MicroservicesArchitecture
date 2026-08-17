using Certification.Application.Abstractions;
namespace Certification.Application.Certificates.ListStudentCertificates;
public sealed class ListStudentCertificatesHandler(
    ICertificateRepository certificates,
    ICurrentActor currentActor)
{
    public async Task<IReadOnlyList<CertificateView>> HandleAsync(
        ListStudentCertificatesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var owned = await certificates.ListByStudentAsync(
            currentActor.StudentId, cancellationToken);

        return [.. owned.Select(CertificateView.From)];
    }
}