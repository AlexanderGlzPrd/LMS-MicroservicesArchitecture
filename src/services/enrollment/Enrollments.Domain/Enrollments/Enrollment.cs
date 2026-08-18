using Enrollments.Domain.Enrollments.Exceptions;
namespace Enrollments.Domain.Enrollments;

public sealed class Enrollment
{
    private Enrollment()
    {
    }

    /// <exception cref="InvalidEnrollmentIdentityException">Si alguna de las dos identidades es Guid.Empty.</exception>
    public static Enrollment GrantFree(
        EnrollmentId id,
        StudentId studentId,
        CourseId courseId,
        DateTimeOffset enrolledAt)
    {
        EnsureNotEmpty(studentId.Value, nameof(studentId));
        EnsureNotEmpty(courseId.Value, nameof(courseId));

        return new Enrollment
        {
            Id = id,
            StudentId = studentId,
            CourseId = courseId,
            Type = EnrollmentType.Free,
            EnrolledAt = enrolledAt,
        };
    }

    public static Enrollment GrantPaid(
        EnrollmentId id,
        StudentId studentId,
        CourseId courseId,
        DateTimeOffset grantedAt)
    {
        EnsureNotEmpty(studentId.Value, nameof(studentId));
        EnsureNotEmpty(courseId.Value, nameof(courseId));

        return new Enrollment
        {
            Id = id,
            StudentId = studentId,
            CourseId = courseId,
            Type = EnrollmentType.Paid,
            EnrolledAt = grantedAt,
        };
    }

    private static void EnsureNotEmpty(Guid value, string identityName)
    {
        if (value == Guid.Empty)
        {
            throw new InvalidEnrollmentIdentityException(identityName);
        }
    }

    public EnrollmentId Id { get; private set; }

    public StudentId StudentId { get; private set; }

    public CourseId CourseId { get; private set; }

    public EnrollmentType Type { get; private set; }

    public DateTimeOffset EnrolledAt { get; private set; }
}
