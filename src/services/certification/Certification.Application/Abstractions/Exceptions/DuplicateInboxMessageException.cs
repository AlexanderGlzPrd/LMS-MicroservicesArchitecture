namespace Certification.Application.Abstractions.Exceptions;
public sealed class DuplicateInboxMessageException(Exception innerException)
    : Exception("Ese mensaje ya habia sido procesado.", innerException);
