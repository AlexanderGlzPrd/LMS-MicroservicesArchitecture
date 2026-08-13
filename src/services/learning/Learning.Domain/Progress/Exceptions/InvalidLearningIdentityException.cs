using Learning.Domain.Abstractions;

namespace Learning.Domain.Progress.Exceptions;

public sealed class InvalidLearningIdentityException(string identityName)
    : DomainException($"La identidad '{identityName}' del progreso no puede ser un identificador vacio.")
{
    public string IdentityName { get; } = identityName;
}
