namespace Certification.Application.Abstractions.Exceptions;
public sealed class DuplicatePendingIssuanceException(Exception innerException)
    : Exception("Esa Finalizacion ya tiene una emision pendiente.", innerException);