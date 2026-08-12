using CourseAuthoring.Application.Abstractions;

namespace CourseAuthoring.Application.Courses.PublishCourse;

public sealed class PublishCourseHandler(
    ICourseRepository courses,
    IUnitOfWork unitOfWork,
    ICurrentActor currentActor,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Devuelve <c>null</c> si el curso no existe.
    /// </summary>
    public async Task<CourseView?> HandleAsync(
        PublishCourseCommand command,
        CancellationToken cancellationToken)
    {
        var course = await courses.GetByIdAsync(command.CourseId, cancellationToken);

        if (course is null)
        {
            return null;
        }

        course.Publish(currentActor.InstructorId, timeProvider.GetUtcNow());

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CourseView.From(course);
    }
}
