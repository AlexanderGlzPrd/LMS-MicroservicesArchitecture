namespace PaidEnrollment.Application.Abstractions.Exceptions;
public sealed class DuplicateActivePurchaseException(Exception innerException)
    : Exception("Ya existe una compra que bloquea ese par de estudiante y curso.", innerException);