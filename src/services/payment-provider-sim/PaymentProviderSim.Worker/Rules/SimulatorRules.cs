using System.Globalization;
using Microsoft.Extensions.Options;
namespace PaymentProviderSim.Worker.Rules;
internal sealed class SimulatorRules
{
    private readonly Dictionary<decimal, SimulatorRule> rulesByAmount;

    public SimulatorRules(IOptions<SimulatorOptions> options, ILogger<SimulatorRules> logger)
    {
        SilentReplyCount = options.Value.SilentReplyCount;
        rulesByAmount = [];

        foreach (var (amountText, ruleName) in options.Value.RulesByAmount)
        {
            if (!decimal.TryParse(
                    amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            {
                logger.LogWarning(
                    "La regla '{Rule}' declara un importe no numerico: '{Amount}'. Se ignora.",
                    ruleName,
                    amountText);

                continue;
            }

            if (!Enum.TryParse<SimulatorRule>(ruleName, out var rule) || rule == SimulatorRule.None)
            {
                logger.LogWarning(
                    "El importe '{Amount}' declara una regla desconocida: '{Rule}'. Se ignora.",
                    amountText,
                    ruleName);

                continue;
            }

            rulesByAmount[amount] = rule;
        }
    }

    public int SilentReplyCount { get; }

    public SimulatorRule For(decimal amount) =>
        rulesByAmount.TryGetValue(amount, out var rule) ? rule : SimulatorRule.None;
}