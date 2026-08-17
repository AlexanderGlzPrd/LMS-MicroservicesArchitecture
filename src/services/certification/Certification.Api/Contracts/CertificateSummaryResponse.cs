using Certification.Application.Certificates;
namespace Certification.Api.Contracts;
public sealed record CertificateSummaryResponse(
    Guid CertificateId,
    Guid CourseId,
    string CourseTitle,
    DateTimeOffset CompletedAt,
    DateTimeOffset IssuedAt)
{
    public static CertificateSummaryResponse From(CertificateView view) => new(
        view.CertificateId,
        view.CourseId,
        view.CourseTitle,
        view.CompletedAt,
        view.IssuedAt);
}
