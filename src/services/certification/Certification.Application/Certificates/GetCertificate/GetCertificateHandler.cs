using Certification.Application.Abstractions;
namespace Certification.Application.Certificates.GetCertificate;
public sealed class GetCertificateHandler(
    ICertificateRepository certificates,
    ICurrentActor currentActor)
{
    public async Task<CertificateView?> HandleAsync(
        GetCertificateQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var certificate = await certificates.FindByIdAsync(query.CertificateId, cancellationToken);

        return certificate is null || certificate.StudentId != currentActor.StudentId
            ? null
            : CertificateView.From(certificate);
    }
}