using Learning.Application.Abstractions;
using Learning.Domain.Progress;
namespace Learning.Integration.Tests;
internal sealed class RacingCourseProgressRepository(
    ICourseProgressRepository inner,
    LearningApiFactory factory) : ICourseProgressRepository
{
    public async Task<CourseProgress?> FindAsync(
        StudentId studentId,
        CourseId courseId,
        CancellationToken cancellationToken)
    {
        var progress = await inner.FindAsync(studentId, courseId, cancellationToken);

        var hook = factory.TakeRaceHook();

        if (hook is not null)
        {
            await hook();
        }

        return progress;
    }

    public void Add(CourseProgress progress) => inner.Add(progress);
}
