namespace PaidEnrollment.Infrastructure.Pricing;
public sealed class PurchaseOptions
{
    public const string SectionName = "Purchase";

    public string Currency { get; set; } = "PEN";

    public Dictionary<string, string> AmountsByCourse { get; set; } = [];
}