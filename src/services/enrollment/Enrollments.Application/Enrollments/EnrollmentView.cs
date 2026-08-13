using Enrollments.Domain.Enrollments;
namespace Enrollments.Application.Enrollments;

public sealed record EnrollmentView(
    Guid Id,
    Guid StudentId,
    Guid CourseId,
    string Type,
    DateTimeOffset EnrolledAt)
{
    public static EnrollmentView From(Enrollment enrollment) => new(
        enrollment.Id.Value,
        enrollment.StudentId.Value,
        enrollment.CourseId.Value,
        enrollment.Type.ToString(),
        enrollment.EnrolledAt);
}