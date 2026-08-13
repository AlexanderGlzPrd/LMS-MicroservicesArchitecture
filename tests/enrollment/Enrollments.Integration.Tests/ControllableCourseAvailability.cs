using Enrollments.Application.Abstractions;
using Enrollments.Domain.Enrollments;
namespace Enrollments.Integration.Tests;

public sealed class ControllableCourseAvailability : ICourseAvailability
{
    public CourseAvailability Result { get; set; } = CourseAvailability.Available;

    public int CheckCount { get; private set; }

    public void Reset()
    {
        Result = CourseAvailability.Available;
        CheckCount = 0;
    }

    public Task<CourseAvailability> CheckAsync(
        CourseId courseId,
        CancellationToken cancellationToken)
    {
        CheckCount++;

        return Task.FromResult(Result);
    }
}
