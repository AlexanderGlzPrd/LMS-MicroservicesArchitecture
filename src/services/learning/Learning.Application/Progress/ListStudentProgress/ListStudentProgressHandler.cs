using Learning.Application.Abstractions;
namespace Learning.Application.Progress.ListStudentProgress;

public sealed class ListStudentProgressHandler(
    ICourseProgressReadModel readModel,
    ICurrentActor currentActor)
{
    public Task<IReadOnlyList<CourseProgressView>> HandleAsync(
        ListStudentProgressQuery query,
        CancellationToken cancellationToken) =>
        readModel.ListByStudentAsync(
            currentActor.StudentId,
            query.Status,
            cancellationToken);
}
