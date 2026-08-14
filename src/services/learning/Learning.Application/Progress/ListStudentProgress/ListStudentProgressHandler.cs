using Learning.Application.Abstractions;
namespace Learning.Application.Progress.ListStudentProgress;

public sealed class ListStudentProgressHandler(
    ICourseProgressRepository progresses,
    ICurrentActor currentActor)
{
    public async Task<IReadOnlyList<CourseProgressView>> HandleAsync(
        ListStudentProgressQuery query,
        CancellationToken cancellationToken)
    {
        var studentProgress = await progresses.ListByStudentAsync(
            currentActor.StudentId,
            query.Status,
            cancellationToken);

        return [.. studentProgress.Select(CourseProgressView.From)];
    }
}