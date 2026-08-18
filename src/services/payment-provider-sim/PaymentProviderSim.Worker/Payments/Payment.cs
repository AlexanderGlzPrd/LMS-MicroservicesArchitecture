namespace PaymentProviderSim.Worker.Payments;
internal sealed class Payment
{
    private Payment()
    {
    }

    public static Payment Authorize(
        Guid paymentId,
        Guid purchaseId,
        decimal amount,
        string currency,
        DateTimeOffset now) =>
        new()
        {
            PaymentId = paymentId,
            PurchaseId = purchaseId,
            Amount = amount,
            Currency = currency,
            Status = PaymentStatus.Authorized,
            AuthorizedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        };

    public static Payment Decline(
        Guid paymentId,
        Guid purchaseId,
        decimal amount,
        string currency,
        string reason,
        DateTimeOffset now) =>
        new()
        {
            PaymentId = paymentId,
            PurchaseId = purchaseId,
            Amount = amount,
            Currency = currency,
            Status = PaymentStatus.Declined,
            LastFailureReason = reason,
            CreatedAt = now,
            UpdatedAt = now,
        };

    public Guid PaymentId { get; private set; }

    public Guid PurchaseId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public PaymentStatus Status { get; private set; }

    public DateTimeOffset? AuthorizedAt { get; private set; }

    public DateTimeOffset? CapturedAt { get; private set; }

    public DateTimeOffset? VoidedAt { get; private set; }

    public DateTimeOffset? RefundedAt { get; private set; }

    public string? LastFailureReason { get; private set; }

    public int SuppressedReplyCount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void Capture(DateTimeOffset now)
    {
        Status = PaymentStatus.Captured;
        CapturedAt = now;
        UpdatedAt = now;
    }
    public void FailCapture(string reason, DateTimeOffset now)
    {
        Status = PaymentStatus.CaptureFailed;
        LastFailureReason = reason;
        UpdatedAt = now;
    }

    public void Void(DateTimeOffset now)
    {
        Status = PaymentStatus.Voided;
        VoidedAt = now;
        UpdatedAt = now;
    }

    public void Refund(DateTimeOffset now)
    {
        Status = PaymentStatus.Refunded;
        RefundedAt = now;
        UpdatedAt = now;
    }

    public void FailRefund(string reason, DateTimeOffset now)
    {
        LastFailureReason = reason;
        UpdatedAt = now;
    }

    public void RecordSuppressedReply(DateTimeOffset now)
    {
        SuppressedReplyCount++;
        UpdatedAt = now;
    }
}