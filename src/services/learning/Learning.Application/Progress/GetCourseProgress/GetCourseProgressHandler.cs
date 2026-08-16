using Learning.Application.Abstractions;

namespace Learning.Application.Progress.GetCourseProgress;

public sealed class GetCourseProgressHandler(
    ICourseProgressReadModel readModel,
    ICurrentActor currentActor)
{
    public Task<CourseProgressView?> HandleAsync(
        GetCourseProgressQuery query,
        CancellationToken cancellationToken) =>
        readModel.FindAsync(
            currentActor.StudentId,
            query.CourseId,
            cancellationToken);
}
