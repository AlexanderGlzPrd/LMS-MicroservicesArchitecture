using PaidEnrollment.Domain.Abstractions;
namespace PaidEnrollment.Domain.Purchases.Exceptions;
public sealed class InvalidPurchaseIdentityException(string identityName)
    : DomainException($"La identidad '{identityName}' no puede estar vacia.")
{
    public string IdentityName { get; } = identityName;
}
