using CourseAuthoring.Application.Abstractions;
namespace CourseAuthoring.Application.Courses.RemoveLesson;

public sealed class RemoveLessonHandler(
    ICourseRepository courses,
    IUnitOfWork unitOfWork,
    ICurrentActor currentActor)
{

    public async Task<bool> HandleAsync(
        RemoveLessonCommand command,
        CancellationToken cancellationToken)
    {
        var course = await courses.GetByIdAsync(command.CourseId, cancellationToken);

        if (course is null)
        {
            return false;
        }

        course.RemoveLesson(currentActor.InstructorId, command.LessonId);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
