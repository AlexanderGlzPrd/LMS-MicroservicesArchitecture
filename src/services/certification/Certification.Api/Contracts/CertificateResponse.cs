using Certification.Application.Certificates;
namespace Certification.Api.Contracts;
public sealed record CertificateResponse(
    Guid CertificateId,
    Guid StudentId,
    Guid CourseId,
    string StudentName,
    string CourseTitle,
    DateTimeOffset CompletedAt,
    DateTimeOffset IssuedAt,
    string Issuer)
{
    public static CertificateResponse From(CertificateView view) => new(
        view.CertificateId,
        view.StudentId,
        view.CourseId,
        view.StudentName,
        view.CourseTitle,
        view.CompletedAt,
        view.IssuedAt,
        view.Issuer);
}
