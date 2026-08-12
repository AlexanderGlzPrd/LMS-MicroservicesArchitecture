using CourseAuthoring.Application.Courses.GetCourseById;
using CourseAuthoring.Application.Tests.Fakes;
using CourseAuthoring.Domain.Courses;
using CourseAuthoring.Domain.Courses.Exceptions;

namespace CourseAuthoring.Application.Tests;

public sealed class GetCourseByIdHandlerTests
{
    private static readonly InstructorId Owner = new(Guid.CreateVersion7());
    private static readonly InstructorId Intruder = new(Guid.CreateVersion7());
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PublishedAt = new(2026, 8, 12, 13, 0, 0, TimeSpan.Zero);

    private readonly InMemoryCourseRepository courses = new();

    [Fact]
    public async Task Propietario_RecibeElContenidoDeTrabajo()
    {
        var course = SeedCourse();
        course.Rename(Owner, "Titulo de trabajo");

        var view = await Handler(Owner).HandleAsync(
            new GetCourseByIdQuery(course.Id),
            CancellationToken.None);

        Assert.NotNull(view);
        Assert.Equal("Titulo de trabajo", view.Title);
        Assert.Equal(["Uno", "Dos"], view.Lessons.Select(lesson => lesson.Title));
        Assert.Equal([1, 2], view.Lessons.Select(lesson => lesson.Position));
        Assert.Equal(PublishedAt, view.PublishedAt);
    }

    [Fact]
    public async Task ActorNoPropietario_Lanza()
    {
        var course = SeedCourse();

        await Assert.ThrowsAsync<CourseOwnershipException>(() => Handler(Intruder).HandleAsync(
            new GetCourseByIdQuery(course.Id),
            CancellationToken.None));
    }

    [Fact]
    public async Task CursoInexistente_DevuelveNull()
    {
        var view = await Handler(Owner).HandleAsync(
            new GetCourseByIdQuery(new CourseId(Guid.CreateVersion7())),
            CancellationToken.None);

        Assert.Null(view);
    }

    private Course SeedCourse()
    {
        var course = Course.Create(
            new CourseId(Guid.CreateVersion7()), Owner, "Microservicios con .NET", CreatedAt);

        course.AddLesson(Owner, new LessonId(Guid.CreateVersion7()), "Uno", "Descripcion",
            "https://videos.example.com/1.mp4");
        course.AddLesson(Owner, new LessonId(Guid.CreateVersion7()), "Dos", "Descripcion",
            "https://videos.example.com/2.mp4");
        course.Publish(Owner, PublishedAt);

        courses.Add(course);

        return course;
    }

    private GetCourseByIdHandler Handler(InstructorId actor) =>
        new(courses, new StubCurrentActor(actor));
}
