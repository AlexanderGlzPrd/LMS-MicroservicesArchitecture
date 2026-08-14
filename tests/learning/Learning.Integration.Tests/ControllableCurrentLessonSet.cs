using Learning.Application.Abstractions;
using Learning.Domain.Progress;
namespace Learning.Integration.Tests;

public sealed class ControllableCurrentLessonSet : ICurrentLessonSet
{
    public CurrentLessonSet Result { get; private set; } = CurrentLessonSet.Unknown;
    public int GetCount { get; private set; }

    public void Publish(params LessonId[] lessonIds) =>
        Result = CurrentLessonSet.Available(new HashSet<LessonId>(lessonIds));

    public void NotAvailable() => Result = CurrentLessonSet.NotAvailable;

    public void Unknown() => Result = CurrentLessonSet.Unknown;

    public void Reset()
    {
        Result = CurrentLessonSet.Unknown;
        GetCount = 0;
    }

    public Task<CurrentLessonSet> GetAsync(CourseId courseId, CancellationToken cancellationToken)
    {
        GetCount++;

        return Task.FromResult(Result);
    }
}
