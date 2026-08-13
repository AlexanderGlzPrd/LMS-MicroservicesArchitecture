using Enrollments.Application.Abstractions;
using Enrollments.Domain.Enrollments;
namespace Enrollments.Application.Tests.Fakes;

internal sealed class StubCourseAvailability(CourseAvailability result) : ICourseAvailability
{
    public int CheckCount { get; private set; }

    public Task<CourseAvailability> CheckAsync(
        CourseId courseId,
        CancellationToken cancellationToken)
    {
        CheckCount++;

        return Task.FromResult(result);
    }
}
