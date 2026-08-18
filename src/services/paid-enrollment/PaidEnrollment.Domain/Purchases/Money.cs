using PaidEnrollment.Domain.Purchases.Exceptions;
namespace PaidEnrollment.Domain.Purchases;

public readonly record struct Money
{
    private const int CurrencyLength = 3;

    private const int Scale = 2;

    public Money(decimal amount, string currency)
    {
        if (amount <= 0m)
        {
            throw new InvalidPurchaseAmountException("debe ser mayor que cero.");
        }

        if (decimal.Round(amount, Scale) != amount)
        {
            throw new InvalidPurchaseAmountException($"admite como mucho {Scale} decimales.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != CurrencyLength)
        {
            throw new InvalidPurchaseAmountException(
                $"la moneda debe tener {CurrencyLength} caracteres.");
        }

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public decimal Amount { get; }

    public string Currency { get; }
}
