using CourseAuthoring.Application.Abstractions;
using CourseAuthoring.Domain.Courses;

namespace CourseAuthoring.Application.Courses.CreateCourse;

public sealed class CreateCourseHandler(
    ICourseRepository courses,
    IUnitOfWork unitOfWork,
    ICurrentActor currentActor,
    TimeProvider timeProvider)
{
    public async Task<CourseView> HandleAsync(
        CreateCourseCommand command,
        CancellationToken cancellationToken)
    {
        var course = Course.Create(
            new CourseId(Guid.CreateVersion7()),
            currentActor.InstructorId,
            command.Title,
            timeProvider.GetUtcNow());

        courses.Add(course);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CourseView.From(course);
    }
}
