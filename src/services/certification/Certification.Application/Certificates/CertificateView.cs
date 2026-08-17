using Certification.Domain.Certificates;
namespace Certification.Application.Certificates;
public sealed record CertificateView(
    Guid CertificateId,
    Guid StudentId,
    Guid CourseId,
    string StudentName,
    string CourseTitle,
    DateTimeOffset CompletedAt,
    DateTimeOffset IssuedAt,
    string Issuer)
{
    public static CertificateView From(Certificate certificate) => new(
        certificate.CertificateId.Value,
        certificate.StudentId,
        certificate.CourseId,
        certificate.StudentName,
        certificate.CourseTitle,
        certificate.CompletedAt,
        certificate.IssuedAt,
        certificate.Issuer);
}