using CourseAuthoring.Application.Abstractions;
using CourseAuthoring.Domain.Courses;

namespace CourseAuthoring.Application.Courses.AddLesson;

public sealed class AddLessonHandler(
    ICourseRepository courses,
    IUnitOfWork unitOfWork,
    ICurrentActor currentActor)
{
    /// <summary>
    /// Devuelve <c>null</c> si el curso no existe.
    /// </summary>
    public async Task<LessonView?> HandleAsync(
        AddLessonCommand command,
        CancellationToken cancellationToken)
    {
        var course = await courses.GetByIdAsync(command.CourseId, cancellationToken);

        if (course is null)
        {
            return null;
        }

        var lessonId = course.AddLesson(
            currentActor.InstructorId,
            new LessonId(Guid.CreateVersion7()),
            command.Title,
            command.Description,
            command.VideoUrl);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return LessonView.From(course.WorkingLessons.Single(lesson => lesson.Id == lessonId));
    }
}
