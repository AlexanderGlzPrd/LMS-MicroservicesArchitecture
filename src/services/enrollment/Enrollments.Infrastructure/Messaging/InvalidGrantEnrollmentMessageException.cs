namespace Enrollments.Infrastructure.Messaging;
internal sealed class InvalidGrantEnrollmentMessageException(string reason)
    : Exception($"El mensaje GrantEnrollmentForCapturedPayment no es valido: {reason}");