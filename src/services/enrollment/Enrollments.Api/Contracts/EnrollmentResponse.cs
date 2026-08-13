using Enrollments.Application.Enrollments;
namespace Enrollments.Api.Contracts;

public sealed record EnrollmentResponse(
    Guid Id,
    Guid StudentId,
    Guid CourseId,
    string Type,
    DateTimeOffset EnrolledAt)
{
    public static EnrollmentResponse From(EnrollmentView view) => new(
        view.Id,
        view.StudentId,
        view.CourseId,
        view.Type,
        view.EnrolledAt);
}
