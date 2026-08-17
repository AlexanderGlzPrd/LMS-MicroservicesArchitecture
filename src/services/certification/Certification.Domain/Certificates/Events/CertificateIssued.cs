namespace Certification.Domain.Certificates.Events;
public sealed record CertificateIssued( // Evento interno
    CertificateId CertificateId,
    Guid StudentId,
    Guid CourseId,
    DateTimeOffset IssuedAt);