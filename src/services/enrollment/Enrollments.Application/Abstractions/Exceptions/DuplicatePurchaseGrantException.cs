namespace Enrollments.Application.Abstractions.Exceptions;
public sealed class DuplicatePurchaseGrantException(Exception innerException)
    : Exception("Ya existe una concesion registrada para esa compra.", innerException);