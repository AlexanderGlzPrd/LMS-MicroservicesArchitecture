using Learning.Application.Abstractions;

namespace Learning.Application.Progress.GetCourseProgress;

public sealed class GetCourseProgressHandler(
    ICourseProgressRepository progresses,
    ICurrentActor currentActor)
{
    public async Task<CourseProgressView?> HandleAsync(
        GetCourseProgressQuery query,
        CancellationToken cancellationToken)
    {
        var progress = await progresses.FindAsync(
            currentActor.StudentId,
            query.CourseId,
            cancellationToken);

        return progress is null ? null : CourseProgressView.From(progress);
    }
}
