namespace PaidEnrollment.Infrastructure.Acl;
internal sealed record EnrollmentResponse(
    Guid Id,
    Guid StudentId,
    Guid CourseId,
    string Type,
    DateTimeOffset EnrolledAt);
