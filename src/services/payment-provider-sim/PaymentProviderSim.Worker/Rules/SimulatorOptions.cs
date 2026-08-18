namespace PaymentProviderSim.Worker.Rules;
public sealed class SimulatorOptions
{
    public const string SectionName = "Simulator";

    public int SilentReplyCount { get; set; } = 1;

    public Dictionary<string, string> RulesByAmount { get; set; } = [];
}