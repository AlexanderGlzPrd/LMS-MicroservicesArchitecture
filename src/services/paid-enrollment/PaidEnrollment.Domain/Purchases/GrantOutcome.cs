namespace PaidEnrollment.Domain.Purchases;
public enum GrantOutcome
{
    Created = 1,
    AlreadyExistedThisPurchase = 2,
    AlreadyExistedOther = 3,
    Rejected = 4,
}
