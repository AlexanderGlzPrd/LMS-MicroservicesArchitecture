using Certification.Application.Certificates;
namespace Certification.Api.Contracts;
public sealed record CertificateVerificationResponse(
    Guid CertificateId,
    bool Valid,
    string StudentName,
    string CourseTitle,
    DateTimeOffset CompletedAt,
    string Issuer)
{
    public static CertificateVerificationResponse From(CertificateVerificationView view) => new(
        view.CertificateId,
        view.Valid,
        view.StudentName,
        view.CourseTitle,
        view.CompletedAt,
        view.Issuer);
}
