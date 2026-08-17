namespace Certification.Infrastructure.Issuance;
public sealed class CertificateIssuanceOptions
{
    public const string SectionName = "Certification";

    public string Issuer { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public int PollingIntervalSeconds { get; set; } = 5;

    public int BatchSize { get; set; } = 20;

    public int DiagnosticsTimeoutSeconds { get; set; } = 5;
}