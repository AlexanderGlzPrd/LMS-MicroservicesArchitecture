using Learning.Application.Abstractions;
using Learning.Application.Abstractions.Exceptions;
using Learning.Domain.Progress;
namespace Learning.Application.Progress.MarkLessonCompleted;

public sealed class MarkLessonCompletedHandler(
    ICourseProgressRepository progresses,
    IUnitOfWork unitOfWork,
    ICurrentLessonSet currentLessonSet,
    ICurrentActor currentActor,
    TimeProvider timeProvider)
{
    public async Task<CourseProgressView> HandleAsync(
        MarkLessonCompletedCommand command,
        CancellationToken cancellationToken)
    {
        var studentId = currentActor.StudentId;

        var lessonSet = await currentLessonSet.GetAsync(command.CourseId, cancellationToken);

        if (lessonSet.Status is CurrentLessonSetStatus.NotAvailable)
        {
            throw new CourseNotAvailableException(command.CourseId);
        }

        if (lessonSet.Status is CurrentLessonSetStatus.Unknown)
        {
            throw new CurrentLessonSetUnknownException(command.CourseId);
        }

        try
        {
            return await ApplyAsync(studentId, command, lessonSet.LessonIds, cancellationToken);
        }
        catch (ConcurrentCourseProgressException)
        {
            return await ApplyAsync(studentId, command, lessonSet.LessonIds, cancellationToken);
        }
    }

    private async Task<CourseProgressView> ApplyAsync(
        StudentId studentId,
        MarkLessonCompletedCommand command,
        IReadOnlySet<LessonId> publishedLessonIds,
        CancellationToken cancellationToken)
    {
        var progress = await progresses.FindAsync(studentId, command.CourseId, cancellationToken)
            ?? throw new CourseProgressNotFoundException(studentId, command.CourseId);

        var changed = progress.MarkLessonCompleted(
            command.LessonId,
            publishedLessonIds,
            timeProvider.GetUtcNow());

        if (changed)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return CourseProgressView.From(progress, publishedLessonIds.Count);
    }
}
