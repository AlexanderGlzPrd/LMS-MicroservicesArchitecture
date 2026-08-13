using Enrollments.Domain.Abstractions;

namespace Enrollments.Domain.Enrollments.Exceptions;

public sealed class InvalidEnrollmentIdentityException(string identityName)
    : DomainException($"La identidad '{identityName}' de la matricula no puede ser un identificador vacio.")
{
    public string IdentityName { get; } = identityName;
}
