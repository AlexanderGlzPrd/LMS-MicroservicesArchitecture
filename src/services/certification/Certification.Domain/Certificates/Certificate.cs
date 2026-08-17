using Certification.Domain.Certificates.Events;
using Certification.Domain.Certificates.Exceptions;
namespace Certification.Domain.Certificates;
public sealed class Certificate
{
    private readonly List<CertificateIssued> _domainEvents = [];

    private Certificate()
    {
    }

    public static Certificate Issue(
        CertificateId certificateId,
        Guid studentId,
        Guid courseId,
        string studentName,
        string courseTitle,
        DateTimeOffset completedAt,
        DateTimeOffset issuedAt,
        string issuer)
    {
        EnsureNotEmpty(certificateId.Value, nameof(certificateId));
        EnsureNotEmpty(studentId, nameof(studentId));
        EnsureNotEmpty(courseId, nameof(courseId));

        EnsureComplete(studentName, nameof(studentName));
        EnsureComplete(courseTitle, nameof(courseTitle));

        var certificate = new Certificate
        {
            CertificateId = certificateId,
            StudentId = studentId,
            CourseId = courseId,
            StudentName = studentName,
            CourseTitle = courseTitle,
            CompletedAt = completedAt,
            IssuedAt = issuedAt,
            Issuer = issuer,
        };

        certificate._domainEvents.Add(
            new CertificateIssued(certificateId, studentId, courseId, issuedAt));

        return certificate;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private static void EnsureNotEmpty(Guid value, string identityName)
    {
        if (value == Guid.Empty)
        {
            throw new InvalidCertificationIdentityException(identityName);
        }
    }

    private static void EnsureComplete(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new IncompleteCertificateDataException(fieldName);
        }
    }

    public CertificateId CertificateId { get; private set; }

    public Guid StudentId { get; private set; }

    public Guid CourseId { get; private set; }

    public string StudentName { get; private set; } = string.Empty;

    public string CourseTitle { get; private set; } = string.Empty;

    public DateTimeOffset CompletedAt { get; private set; }

    public DateTimeOffset IssuedAt { get; private set; }

    public string Issuer { get; private set; } = string.Empty;

    public IReadOnlyCollection<CertificateIssued> DomainEvents => _domainEvents.AsReadOnly();
}