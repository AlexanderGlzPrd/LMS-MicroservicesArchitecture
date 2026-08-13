using Learning.Domain.Abstractions;

namespace Learning.Domain.Progress.Exceptions;

public sealed class CompletionNotReadyException()
    : DomainException("El progreso no cumple el criterio de finalizacion: faltan lecciones publicadas por completar.");
