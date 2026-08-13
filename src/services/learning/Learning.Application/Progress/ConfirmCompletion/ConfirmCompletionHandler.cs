using Learning.Application.Abstractions;
using Learning.Application.Abstractions.Exceptions;
namespace Learning.Application.Progress.ConfirmCompletion;

public sealed class ConfirmCompletionHandler(
    ICourseProgressRepository progresses,
    IUnitOfWork unitOfWork,
    ICurrentLessonSet currentLessonSet,
    ICurrentActor currentActor,
    TimeProvider timeProvider)
{
    public async Task<CourseProgressView> HandleAsync(
        ConfirmCompletionCommand command,
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

        var progress = await progresses.FindAsync(studentId, command.CourseId, cancellationToken)
            ?? throw new CourseProgressNotFoundException(studentId, command.CourseId);

        var justSealed = progress.ConfirmCompletion(lessonSet.LessonIds, timeProvider.GetUtcNow());

        if (justSealed)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return CourseProgressView.From(progress);
    }
}
