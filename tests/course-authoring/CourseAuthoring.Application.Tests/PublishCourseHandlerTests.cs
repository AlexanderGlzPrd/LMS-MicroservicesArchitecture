using CourseAuthoring.Application.Courses.PublishCourse;
using CourseAuthoring.Application.Tests.Fakes;
using CourseAuthoring.Domain.Courses;
using CourseAuthoring.Domain.Courses.Exceptions;

namespace CourseAuthoring.Application.Tests;

public sealed class PublishCourseHandlerTests
{
    private static readonly InstructorId Owner = new(Guid.CreateVersion7());
    private static readonly InstructorId Intruder = new(Guid.CreateVersion7());
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PublishedAt = new(2026, 8, 12, 13, 0, 0, TimeSpan.Zero);

    private readonly InMemoryCourseRepository courses = new();
    private readonly NoOpUnitOfWork unitOfWork = new();
    private readonly FixedTimeProvider timeProvider = new(PublishedAt);

    [Fact]
    public async Task ConLecciones_PublicaConLaHoraDelTimeProviderYConfirma()
    {
        var course = SeedCourse(withLesson: true);

        var view = await Handler(Owner).HandleAsync(
            new PublishCourseCommand(course.Id),
            CancellationToken.None);

        Assert.NotNull(view);
        Assert.Equal("Published", view.Status);
        Assert.Equal(PublishedAt, view.PublishedAt);
        Assert.Equal(PublishedAt, view.PublishedContentUpdatedAt);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task SinLecciones_LanzaYNoLlegaAConfirmar()
    {
        var course = SeedCourse(withLesson: false);

        await Assert.ThrowsAsync<CourseHasNoLessonsException>(() => Handler(Owner).HandleAsync(
            new PublishCourseCommand(course.Id),
            CancellationToken.None));

        Assert.Equal(0, unitOfWork.SaveChangesCount);
        Assert.Equal(CourseStatus.Draft, course.Status);
    }

    [Fact]
    public async Task CursoYaPublicado_LanzaYNoLlegaAConfirmar()
    {
        var course = SeedCourse(withLesson: true);
        course.Publish(Owner, PublishedAt);

        await Assert.ThrowsAsync<InvalidCourseStateException>(() => Handler(Owner).HandleAsync(
            new PublishCourseCommand(course.Id),
            CancellationToken.None));

        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task ActorNoPropietario_LanzaYNoLlegaAConfirmar()
    {
        var course = SeedCourse(withLesson: true);

        await Assert.ThrowsAsync<CourseOwnershipException>(() => Handler(Intruder).HandleAsync(
            new PublishCourseCommand(course.Id),
            CancellationToken.None));

        Assert.Equal(0, unitOfWork.SaveChangesCount);
        Assert.Equal(CourseStatus.Draft, course.Status);
    }

    [Fact]
    public async Task CursoInexistente_DevuelveNullYNoConfirma()
    {
        var view = await Handler(Owner).HandleAsync(
            new PublishCourseCommand(new CourseId(Guid.CreateVersion7())),
            CancellationToken.None);

        Assert.Null(view);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    private Course SeedCourse(bool withLesson)
    {
        var course = Course.Create(
            new CourseId(Guid.CreateVersion7()), Owner, "Microservicios con .NET", CreatedAt);

        if (withLesson)
        {
            course.AddLesson(Owner, new LessonId(Guid.CreateVersion7()), "Uno", "Descripcion",
                "https://videos.example.com/1.mp4");
        }

        courses.Add(course);

        return course;
    }

    private PublishCourseHandler Handler(InstructorId actor) =>
        new(courses, unitOfWork, new StubCurrentActor(actor), timeProvider);
}
