namespace PaymentProviderSim.Worker.Payments;
internal enum PaymentStatus
{
    Authorized = 1,
    Declined = 2,
    Captured = 3,
    CaptureFailed = 4,
    Voided = 5,
    Refunded = 6,
}