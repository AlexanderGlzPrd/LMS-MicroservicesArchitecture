using PaymentProviderSim.Contracts.V1;
namespace PaymentProviderSim.Worker.Messaging;
internal static class OutboxContractMapper
{
    public static readonly string PaymentAuthorizedType = typeof(PaymentAuthorized).FullName!;

    public static readonly string PaymentDeclinedType = typeof(PaymentDeclined).FullName!;

    public static readonly string PaymentCapturedType = typeof(PaymentCaptured).FullName!;

    public static readonly string CaptureFailedType = typeof(CaptureFailed).FullName!;

    public static readonly string AuthorizationVoidedType = typeof(AuthorizationVoided).FullName!;

    public static readonly string PaymentRefundedType = typeof(PaymentRefunded).FullName!;

    public static readonly string RefundFailedType = typeof(RefundFailed).FullName!;

    public static readonly string PaymentStatusReportedType = typeof(PaymentStatusReported).FullName!;

    public const string PaymentAuthorizedRoutingKey = "payment-authorized";

    public const string PaymentDeclinedRoutingKey = "payment-declined";

    public const string PaymentCapturedRoutingKey = "payment-captured";

    public const string CaptureFailedRoutingKey = "capture-failed";

    public const string AuthorizationVoidedRoutingKey = "authorization-voided";

    public const string PaymentRefundedRoutingKey = "payment-refunded";

    public const string RefundFailedRoutingKey = "refund-failed";

    public const string PaymentStatusReportedRoutingKey = "payment-status-reported";
}