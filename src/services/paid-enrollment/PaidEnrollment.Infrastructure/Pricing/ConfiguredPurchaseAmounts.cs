using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaidEnrollment.Application.Abstractions;
using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Infrastructure.Pricing;
internal sealed class ConfiguredPurchaseAmounts : IPurchaseAmounts
{
    private readonly Dictionary<Guid, Money> amountsByCourse;

    public ConfiguredPurchaseAmounts(
        IOptions<PurchaseOptions> options,
        ILogger<ConfiguredPurchaseAmounts> logger)
    {
        var currency = options.Value.Currency;

        amountsByCourse = [];

        foreach (var (courseText, amountText) in options.Value.AmountsByCourse)
        {
            if (!Guid.TryParse(courseText, out var courseId) || courseId == Guid.Empty)
            {
                logger.LogWarning(
                    "La tabla de importes declara un curso no valido: '{Course}'. Se ignora.",
                    courseText);

                continue;
            }

            if (!decimal.TryParse(
                    amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            {
                logger.LogWarning(
                    "El curso '{Course}' declara un importe no numerico: '{Amount}'. Se ignora.",
                    courseText,
                    amountText);

                continue;
            }

            amountsByCourse[courseId] = new Money(amount, currency);
        }
    }

    public Money? For(CourseId courseId) =>
        amountsByCourse.TryGetValue(courseId.Value, out var money) ? money : null;
}
