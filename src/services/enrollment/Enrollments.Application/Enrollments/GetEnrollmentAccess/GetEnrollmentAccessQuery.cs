using Enrollments.Domain.Enrollments;
namespace Enrollments.Application.Enrollments.GetEnrollmentAccess;
public sealed record GetEnrollmentAccessQuery(StudentId StudentId, CourseId CourseId);