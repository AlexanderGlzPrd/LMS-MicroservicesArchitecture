using CourseAuthoring.Application.Abstractions;

namespace CourseAuthoring.Application.Courses.RepublishCourse;

public sealed class RepublishCourseHandler(
    ICourseRepository courses,
    IUnitOfWork unitOfWork,
    ICurrentActor currentActor,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Devuelve <c>null</c> si el curso no existe.
    /// </summary>
    public async Task<RepublishResultView?> HandleAsync(
        RepublishCourseCommand command,
        CancellationToken cancellationToken)
    {
        var course = await courses.GetByIdAsync(command.CourseId, cancellationToken);

        if (course is null)
        {
            return null;
        }

        var changed = course.Republish(currentActor.InstructorId, timeProvider.GetUtcNow());

        if (changed)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new RepublishResultView(changed, course.PublishedContentUpdatedAt);
    }
}
