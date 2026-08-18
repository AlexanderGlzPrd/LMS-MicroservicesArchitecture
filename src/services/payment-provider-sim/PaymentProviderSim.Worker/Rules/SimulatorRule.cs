namespace PaymentProviderSim.Worker.Rules;
internal enum SimulatorRule
{
    None = 0,
    DeclineAuthorization = 1,
    SilentAuthorization = 2,
    FailCapture = 3,
    SilentCapture = 4,
    FailRefund = 5,
    SilentRefund = 6,
}