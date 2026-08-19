namespace PaidEnrollment.Infrastructure.Saga;
public sealed class SagaOptions
{
    public const string SectionName = "Saga";

    public int DriverIntervalSeconds { get; set; } = 1;

    public int ReconciliationIntervalSeconds { get; set; } = 5;

    public int StepTimeoutSeconds { get; set; } = 15;

    public int MaxReconciliationAttempts { get; set; } = 3;

    public int MaxPreCheckAttempts { get; set; } = 3;

    public int BatchSize { get; set; } = 20;
}