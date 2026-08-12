using CourseAuthoring.Domain.Abstractions;

namespace CourseAuthoring.Domain.Courses.Exceptions;

public sealed class InvalidCourseStateException(CourseId courseId, CourseStatus actual, CourseStatus expected)
    : DomainException(
        $"El curso '{courseId.Value}' esta en estado {actual}; la operacion exige {expected}.")
{
    public CourseId CourseId { get; } = courseId;

    public CourseStatus Actual { get; } = actual;

    public CourseStatus Expected { get; } = expected;
}
