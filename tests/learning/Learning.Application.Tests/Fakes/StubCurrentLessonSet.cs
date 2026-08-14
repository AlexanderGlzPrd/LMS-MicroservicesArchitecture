using Learning.Application.Abstractions;
using Learning.Domain.Progress;
namespace Learning.Application.Tests.Fakes;

internal sealed class StubCurrentLessonSet(CurrentLessonSet result) : ICurrentLessonSet
{
    public int GetCount { get; private set; }

    public Task<CurrentLessonSet> GetAsync(CourseId courseId, CancellationToken cancellationToken)
    {
        GetCount++;

        return Task.FromResult(result);
    }
}
