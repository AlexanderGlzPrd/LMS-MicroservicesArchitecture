using PaidEnrollment.Domain.Abstractions;
namespace PaidEnrollment.Domain.Purchases.Exceptions;
public sealed class InvalidPurchaseAmountException(string reason)
    : DomainException($"El importe de la compra no es valido: {reason}");
