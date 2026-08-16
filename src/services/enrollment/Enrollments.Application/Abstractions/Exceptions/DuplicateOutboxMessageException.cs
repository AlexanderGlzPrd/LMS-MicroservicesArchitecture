namespace Enrollments.Application.Abstractions.Exceptions;
public sealed class DuplicateOutboxMessageException(Exception innerException)
    : Exception("Ya existe un mensaje de Outbox para esa matricula y ese tipo.", innerException);