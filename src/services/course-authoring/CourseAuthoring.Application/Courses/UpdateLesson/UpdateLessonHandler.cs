using CourseAuthoring.Application.Abstractions;

namespace CourseAuthoring.Application.Courses.UpdateLesson;

public sealed class UpdateLessonHandler(
    ICourseRepository courses,
    IUnitOfWork unitOfWork,
    ICurrentActor currentActor)
{
    /// <summary>
    /// Devuelve <c>null</c> si el curso no existe.
    /// </summary>
    public async Task<LessonView?> HandleAsync(
        UpdateLessonCommand command,
        CancellationToken cancellationToken)
    {
        var course = await courses.GetByIdAsync(command.CourseId, cancellationToken);

        if (course is null)
        {
            return null;
        }

        course.UpdateLesson(
            currentActor.InstructorId,
            command.LessonId,
            command.Title,
            command.Description,
            command.VideoUrl);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return LessonView.From(course.WorkingLessons.Single(lesson => lesson.Id == command.LessonId));
    }
}
