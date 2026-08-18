using PaidEnrollment.Contracts.V1;
namespace PaidEnrollment.Infrastructure.Messaging;
internal static class OutboxContractMapper
{
    public static readonly string AuthorizePaymentType = typeof(AuthorizePayment).FullName!;

    public static readonly string CapturePaymentType = typeof(CapturePayment).FullName!;

    public static readonly string VoidAuthorizationType = typeof(VoidAuthorization).FullName!;

    public static readonly string RefundPaymentType = typeof(RefundPayment).FullName!;

    public static readonly string GetPaymentStatusType = typeof(GetPaymentStatus).FullName!;

    public static readonly string GrantEnrollmentForCapturedPaymentType =
        typeof(GrantEnrollmentForCapturedPayment).FullName!;

    public const string AuthorizePaymentRoutingKey = "authorize-payment";

    public const string CapturePaymentRoutingKey = "capture-payment";

    public const string VoidAuthorizationRoutingKey = "void-authorization";

    public const string RefundPaymentRoutingKey = "refund-payment";

    public const string GetPaymentStatusRoutingKey = "get-payment-status";

    public const string GrantEnrollmentForCapturedPaymentRoutingKey =
        "grant-enrollment-for-captured-payment";
}