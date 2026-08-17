using Certification.Domain.Abstractions;
namespace Certification.Domain.Certificates.Exceptions;
public sealed class InvalidCertificationIdentityException(string identityName)
    : DomainException(
        $"La identidad '{identityName}' del certificado no puede ser un identificador vacio.")
{
    public string IdentityName { get; } = identityName;
}