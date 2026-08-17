using Certification.Domain.Certificates;
namespace Certification.Application.Certificates;
public sealed record CertificateVerificationView(
    Guid CertificateId,
    bool Valid,
    string StudentName,
    string CourseTitle,
    DateTimeOffset CompletedAt,
    string Issuer)
{
    public static CertificateVerificationView From(Certificate certificate) => new(
        certificate.CertificateId.Value,
        true,
        certificate.StudentName,
        certificate.CourseTitle,
        certificate.CompletedAt,
        certificate.Issuer);
}