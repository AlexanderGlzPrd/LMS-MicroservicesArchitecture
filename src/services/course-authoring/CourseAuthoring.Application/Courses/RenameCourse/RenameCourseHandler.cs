using CourseAuthoring.Application.Abstractions;

namespace CourseAuthoring.Application.Courses.RenameCourse;

public sealed class RenameCourseHandler(
    ICourseRepository courses,
    IUnitOfWork unitOfWork,
    ICurrentActor currentActor)
{
    /// <summary>
    /// Devuelve <c>null</c> si el curso no existe.
    /// </summary>
    public async Task<CourseView?> HandleAsync(
        RenameCourseCommand command,
        CancellationToken cancellationToken)
    {
        var course = await courses.GetByIdAsync(command.CourseId, cancellationToken);

        if (course is null)
        {
            return null;
        }

        course.Rename(currentActor.InstructorId, command.Title);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CourseView.From(course);
    }
}
