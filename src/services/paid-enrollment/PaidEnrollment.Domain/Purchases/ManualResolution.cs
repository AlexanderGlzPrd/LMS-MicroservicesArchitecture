namespace PaidEnrollment.Domain.Purchases;
public enum ManualResolution
{
    ResolveAsConfirmed = 1,
    RetryCompensation = 2,
    ResolveAsCompensated = 3,
    CloseWithoutAutomaticAction = 4,
}
