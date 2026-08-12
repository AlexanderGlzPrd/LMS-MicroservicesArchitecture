using CourseAuthoring.Application.Abstractions;

namespace CourseAuthoring.Application.Courses.ReorderLessons;

public sealed class ReorderLessonsHandler(
    ICourseRepository courses,
    IUnitOfWork unitOfWork,
    ICurrentActor currentActor)
{
    /// <summary>
    /// Devuelve <c>null</c> si el curso no existe.
    /// </summary>
    public async Task<IReadOnlyList<LessonView>?> HandleAsync(
        ReorderLessonsCommand command,
        CancellationToken cancellationToken)
    {
        var course = await courses.GetByIdAsync(command.CourseId, cancellationToken);

        if (course is null)
        {
            return null;
        }

        course.ReorderLessons(currentActor.InstructorId, command.LessonIds);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return [.. course.WorkingLessons.Select(LessonView.From)];
    }
}
