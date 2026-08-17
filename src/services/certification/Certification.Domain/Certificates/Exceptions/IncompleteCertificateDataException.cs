using Certification.Domain.Abstractions;
namespace Certification.Domain.Certificates.Exceptions;
public sealed class IncompleteCertificateDataException(string fieldName)
    : DomainException(
        $"El certificado no puede nacer sin '{fieldName}': " +
        "solo se emite con informacion completa.")
{
    public string FieldName { get; } = fieldName;
}