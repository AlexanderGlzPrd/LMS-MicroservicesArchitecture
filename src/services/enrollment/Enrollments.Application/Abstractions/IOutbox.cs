using Enrollments.Domain.Enrollments;
namespace Enrollments.Application.Abstractions;
public interface IOutbox
{
    void EnqueueStudentEnrolled(Enrollment enrollment);
    Task<bool> EnsureStudentEnrolledAsync(Enrollment enrollment, CancellationToken cancellationToken);
    void EnqueueEnrollmentGranted(Guid incomingMessageId, PurchaseGrantEntry entry);
    void EnqueueEnrollmentRejected(Guid incomingMessageId, PurchaseGrantEntry entry);
}