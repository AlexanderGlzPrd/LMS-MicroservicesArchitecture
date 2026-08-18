using PaidEnrollment.Domain.Purchases.Exceptions;
namespace PaidEnrollment.Domain.Purchases;
public sealed class Purchase
{
    private static readonly PurchaseStatus[] Terminal =
    [
        PurchaseStatus.Confirmed,
        PurchaseStatus.Rejected,
        PurchaseStatus.Compensated,
        PurchaseStatus.Closed,
    ];

    private static readonly PurchaseReason[] ReviewReasons =
    [
        PurchaseReason.AuthorizationOutcomeUnknown,
        PurchaseReason.CaptureOutcomeUnknown,
        PurchaseReason.CaptureOutcomeInconsistent,
        PurchaseReason.EnrollmentOutcomeUnknown,
        PurchaseReason.AccessFromAnotherOrigin,
        PurchaseReason.RefundFailed,
        PurchaseReason.CompensationOutcomeUnknown,
    ];

    private Purchase()
    {
    }

    public static Purchase Start(
        PurchaseId id,
        StudentId studentId,
        CourseId courseId,
        PaymentId paymentId,
        Money price,
        DateTimeOffset now)
    {
        EnsureNotEmpty(id.Value, nameof(id));
        EnsureNotEmpty(studentId.Value, nameof(studentId));
        EnsureNotEmpty(courseId.Value, nameof(courseId));
        EnsureNotEmpty(paymentId.Value, nameof(paymentId));

        return new Purchase
        {
            Id = id,
            StudentId = studentId,
            CourseId = courseId,
            PaymentId = paymentId,
            Price = price,
            Status = PurchaseStatus.Started,
            StepStartedAt = now,
            StepAttempts = 0,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public PurchaseId Id { get; private set; }

    public StudentId StudentId { get; private set; }

    public CourseId CourseId { get; private set; }

    public PaymentId PaymentId { get; private set; }

    public Money Price { get; private set; }

    public PurchaseStatus Status { get; private set; }

    public PurchaseReason? Reason { get; private set; }

    public DateTimeOffset? AuthorizedAt { get; private set; }

    public DateTimeOffset? CapturedAt { get; private set; }

    public DateTimeOffset? VoidedAt { get; private set; }

    public DateTimeOffset? RefundedAt { get; private set; }

    public GrantOutcome? GrantOutcome { get; private set; }

    public DateTimeOffset StepStartedAt { get; private set; }

    public int StepAttempts { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsTerminal => Terminal.Contains(Status);

    public bool IsUnderReview => Status == PurchaseStatus.ManualReview;

    public bool HasExpired(DateTimeOffset now, TimeSpan stepTimeout) =>
        now - StepStartedAt >= stepTimeout;

    public void BeginAccessCheck(DateTimeOffset now)
    {
        EnsureStatus(nameof(BeginAccessCheck), PurchaseStatus.Started);

        MoveTo(PurchaseStatus.CheckingAccess, now);
    }

    public void BeginAuthorization(DateTimeOffset now)
    {
        EnsureStatus(nameof(BeginAuthorization), PurchaseStatus.CheckingAccess);

        MoveTo(PurchaseStatus.AuthorizingPayment, now);
    }

    public void RejectAsAlreadyEnrolled(DateTimeOffset now)
    {
        EnsureStatus(nameof(RejectAsAlreadyEnrolled), PurchaseStatus.CheckingAccess);

        Reject(PurchaseReason.AlreadyEnrolled, now);
    }

    public void RejectAsPreCheckUnavailable(DateTimeOffset now)
    {
        EnsureStatus(nameof(RejectAsPreCheckUnavailable), PurchaseStatus.CheckingAccess);

        Reject(PurchaseReason.PreCheckUnavailable, now);
    }

    public void BeginAuthorizationVerification(DateTimeOffset now)
    {
        EnsureStatus(nameof(BeginAuthorizationVerification), PurchaseStatus.AuthorizingPayment);

        MoveTo(PurchaseStatus.VerifyingAuthorizationOutcome, now);
    }

    public void ConfirmAuthorization(DateTimeOffset authorizedAt, DateTimeOffset now)
    {
        EnsureStatus(
            nameof(ConfirmAuthorization),
            PurchaseStatus.AuthorizingPayment,
            PurchaseStatus.VerifyingAuthorizationOutcome);

        AuthorizedAt ??= authorizedAt;

        MoveTo(PurchaseStatus.PaymentAuthorized, now);
    }

    public void RejectAsPaymentDeclined(DateTimeOffset now)
    {
        EnsureStatus(
            nameof(RejectAsPaymentDeclined),
            PurchaseStatus.AuthorizingPayment,
            PurchaseStatus.VerifyingAuthorizationOutcome);

        Reject(PurchaseReason.PaymentDeclined, now);
    }

    public void RejectAsAuthorizationNotFound(DateTimeOffset now)
    {
        EnsureStatus(
            nameof(RejectAsAuthorizationNotFound),
            PurchaseStatus.VerifyingAuthorizationOutcome);

        Reject(PurchaseReason.AuthorizationNotFound, now);
    }

    public void BeginCapture(DateTimeOffset now)
    {
        EnsureStatus(nameof(BeginCapture), PurchaseStatus.PaymentAuthorized);

        MoveTo(PurchaseStatus.CapturingPayment, now);
    }

    public void BeginCaptureVerification(DateTimeOffset now)
    {
        EnsureStatus(nameof(BeginCaptureVerification), PurchaseStatus.CapturingPayment);

        MoveTo(PurchaseStatus.VerifyingCaptureOutcome, now);
    }

    public void ConfirmCapture(DateTimeOffset capturedAt, DateTimeOffset now)
    {
        EnsureStatus(
            nameof(ConfirmCapture),
            PurchaseStatus.CapturingPayment,
            PurchaseStatus.VerifyingCaptureOutcome,
            PurchaseStatus.VerifyingAuthorizationOutcome);

        CapturedAt ??= capturedAt;

        MoveTo(PurchaseStatus.PaymentCaptured, now);
    }

    public void BeginCompensationAfterCaptureFailure(DateTimeOffset now)
    {
        EnsureStatus(
            nameof(BeginCompensationAfterCaptureFailure),
            PurchaseStatus.CapturingPayment,
            PurchaseStatus.VerifyingCaptureOutcome);

        MoveTo(PurchaseStatus.Compensating, now);
    }

    public void BeginEnrollmentGrant(DateTimeOffset now)
    {
        EnsureStatus(nameof(BeginEnrollmentGrant), PurchaseStatus.PaymentCaptured);

        MoveTo(PurchaseStatus.GrantingEnrollment, now);
    }

    public void BeginEnrollmentVerification(DateTimeOffset now)
    {
        EnsureStatus(nameof(BeginEnrollmentVerification), PurchaseStatus.GrantingEnrollment);

        MoveTo(PurchaseStatus.VerifyingEnrollmentOutcome, now);
    }

    public void ConfirmEnrollmentGranted(GrantOutcome outcome, DateTimeOffset now)
    {
        EnsureStatus(
            nameof(ConfirmEnrollmentGranted),
            PurchaseStatus.GrantingEnrollment,
            PurchaseStatus.VerifyingEnrollmentOutcome);

        if (outcome is not (Purchases.GrantOutcome.Created
            or Purchases.GrantOutcome.AlreadyExistedThisPurchase))
        {
            throw new InvalidPurchaseTransitionException(
                Id, Status, $"{nameof(ConfirmEnrollmentGranted)}({outcome})");
        }

        GrantOutcome = outcome;

        MoveTo(PurchaseStatus.EnrollmentGranted, now);
    }

    public void SuspendForAccessFromAnotherOrigin(DateTimeOffset now)
    {
        EnsureStatus(
            nameof(SuspendForAccessFromAnotherOrigin),
            PurchaseStatus.GrantingEnrollment,
            PurchaseStatus.VerifyingEnrollmentOutcome);

        GrantOutcome = Purchases.GrantOutcome.AlreadyExistedOther;
        Reason = PurchaseReason.AccessFromAnotherOrigin;

        MoveTo(PurchaseStatus.ManualReview, now);
    }

    public void BeginCompensationAfterEnrollmentRejected(DateTimeOffset now)
    {
        EnsureStatus(
            nameof(BeginCompensationAfterEnrollmentRejected),
            PurchaseStatus.GrantingEnrollment,
            PurchaseStatus.VerifyingEnrollmentOutcome);

        GrantOutcome = Purchases.GrantOutcome.Rejected;

        MoveTo(PurchaseStatus.Compensating, now);
    }

    public void Confirm(DateTimeOffset now)
    {
        EnsureStatus(nameof(Confirm), PurchaseStatus.EnrollmentGranted);

        MoveTo(PurchaseStatus.Confirmed, now);
    }

    public void CompleteCompensation(PurchaseReason reason, DateTimeOffset now)
    {
        EnsureStatus(nameof(CompleteCompensation), PurchaseStatus.Compensating);

        if (reason is not (PurchaseReason.AuthorizationVoided or PurchaseReason.PaymentRefunded))
        {
            throw new InvalidPurchaseTransitionException(
                Id, Status, $"{nameof(CompleteCompensation)}({reason})");
        }

        Reason = reason;

        MoveTo(PurchaseStatus.Compensated, now);
    }

    public void SuspendForReview(PurchaseReason reason, DateTimeOffset now)
    {
        EnsureStatus(
            nameof(SuspendForReview),
            PurchaseStatus.VerifyingAuthorizationOutcome,
            PurchaseStatus.VerifyingCaptureOutcome,
            PurchaseStatus.VerifyingEnrollmentOutcome,
            PurchaseStatus.Compensating);

        if (!ReviewReasons.Contains(reason))
        {
            throw new InvalidPurchaseTransitionException(
                Id, Status, $"{nameof(SuspendForReview)}({reason})");
        }

        Reason = reason;

        MoveTo(PurchaseStatus.ManualReview, now);
    }

    public void RestoreEvidence(
        DateTimeOffset? authorizedAt,
        DateTimeOffset? capturedAt,
        DateTimeOffset? voidedAt,
        DateTimeOffset? refundedAt,
        DateTimeOffset now)
    {
        AuthorizedAt ??= authorizedAt;
        CapturedAt ??= capturedAt;
        VoidedAt ??= voidedAt;
        RefundedAt ??= refundedAt;

        UpdatedAt = now;
    }

    public void RegisterGrantOutcome(GrantOutcome outcome, DateTimeOffset now)
    {
        GrantOutcome = outcome;
        UpdatedAt = now;
    }

    public void RegisterStepAttempt(DateTimeOffset now)
    {
        StepAttempts++;
        StepStartedAt = now;
        UpdatedAt = now;
    }

    public void ResolveAsConfirmed(DateTimeOffset now)
    {
        EnsureStatus(nameof(ResolveAsConfirmed), PurchaseStatus.ManualReview);

        var proven = CapturedAt is not null
            && RefundedAt is null
            && VoidedAt is null
            && GrantOutcome is Purchases.GrantOutcome.Created
                or Purchases.GrantOutcome.AlreadyExistedThisPurchase;

        if (!proven)
        {
            throw new InvalidPurchaseTransitionException(Id, Status, nameof(ResolveAsConfirmed));
        }

        Reason = null;

        MoveTo(PurchaseStatus.Confirmed, now);
    }

    public void RetryCompensation(DateTimeOffset now)
    {
        EnsureStatus(nameof(RetryCompensation), PurchaseStatus.ManualReview);

        if (AuthorizedAt is null)
        {
            throw new InvalidPurchaseTransitionException(Id, Status, nameof(RetryCompensation));
        }

        Reason = null;

        MoveTo(PurchaseStatus.Compensating, now);
    }

    public void ResolveAsCompensated(DateTimeOffset now)
    {
        EnsureStatus(nameof(ResolveAsCompensated), PurchaseStatus.ManualReview);

        if (RefundedAt is null && VoidedAt is null)
        {
            throw new InvalidPurchaseTransitionException(Id, Status, nameof(ResolveAsCompensated));
        }

        Reason = PurchaseReason.ManuallyResolved;

        MoveTo(PurchaseStatus.Compensated, now);
    }

    public void CloseWithoutAutomaticAction(DateTimeOffset now)
    {
        EnsureStatus(nameof(CloseWithoutAutomaticAction), PurchaseStatus.ManualReview);

        Reason = PurchaseReason.ClosedByOperator;

        MoveTo(PurchaseStatus.Closed, now);
    }

    private void Reject(PurchaseReason reason, DateTimeOffset now)
    {
        Reason = reason;

        MoveTo(PurchaseStatus.Rejected, now);
    }

    private void MoveTo(PurchaseStatus status, DateTimeOffset now)
    {
        Status = status;
        StepStartedAt = now;
        StepAttempts = 0;
        UpdatedAt = now;
    }

    private void EnsureStatus(string transition, params PurchaseStatus[] allowed)
    {
        if (!allowed.Contains(Status))
        {
            throw new InvalidPurchaseTransitionException(Id, Status, transition);
        }
    }

    private static void EnsureNotEmpty(Guid value, string identityName)
    {
        if (value == Guid.Empty)
        {
            throw new InvalidPurchaseIdentityException(identityName);
        }
    }
}
