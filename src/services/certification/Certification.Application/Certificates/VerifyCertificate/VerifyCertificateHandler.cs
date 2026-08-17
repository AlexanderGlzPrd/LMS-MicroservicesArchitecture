using Certification.Application.Abstractions;
namespace Certification.Application.Certificates.VerifyCertificate;
public sealed class VerifyCertificateHandler(ICertificateRepository certificates)
{
    public async Task<CertificateVerificationView?> HandleAsync(
        VerifyCertificateQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var certificate = await certificates.FindByIdAsync(query.CertificateId, cancellationToken);

        return certificate is null ? null : CertificateVerificationView.From(certificate);
    }
}