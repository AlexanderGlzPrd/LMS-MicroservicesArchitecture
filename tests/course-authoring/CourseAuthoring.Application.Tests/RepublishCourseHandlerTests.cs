using CourseAuthoring.Application.Courses.RepublishCourse;
using CourseAuthoring.Application.Tests.Fakes;
using CourseAuthoring.Domain.Courses;
using CourseAuthoring.Domain.Courses.Exceptions;

namespace CourseAuthoring.Application.Tests;

public sealed class RepublishCourseHandlerTests
{
    private static readonly InstructorId Owner = new(Guid.CreateVersion7());
    private static readonly InstructorId Intruder = new(Guid.CreateVersion7());
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PublishedAt = new(2026, 8, 12, 13, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset RepublishedAt = new(2026, 8, 12, 14, 0, 0, TimeSpan.Zero);

    private readonly InMemoryCourseRepository courses = new();
    private readonly NoOpUnitOfWork unitOfWork = new();
    private readonly FixedTimeProvider timeProvider = new(RepublishedAt);

    [Fact]
    public async Task SinCambios_DevuelveChangedFalseYNoConfirmaLaUnidadDeTrabajo()
    {
        var course = SeedPublishedCourse();

        var view = await Handler(Owner).HandleAsync(
            new RepublishCourseCommand(course.Id),
            CancellationToken.None);

        Assert.NotNull(view);
        Assert.False(view.Changed);
        Assert.Equal(PublishedAt, view.PublishedContentUpdatedAt);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task ConCambios_DevuelveChangedTrueYConfirma()
    {
        var course = SeedPublishedCourse();
        course.Rename(Owner, "Titulo nuevo");

        var view = await Handler(Owner).HandleAsync(
            new RepublishCourseCommand(course.Id),
            CancellationToken.None);

        Assert.NotNull(view);
        Assert.True(view.Changed);
        Assert.Equal(RepublishedAt, view.PublishedContentUpdatedAt);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
        Assert.Equal(PublishedAt, course.PublishedAt);
    }

    [Fact]
    public async Task SobreBorrador_LanzaYNoLlegaAConfirmar()
    {
        var course = Course.Create(
            new CourseId(Guid.CreateVersion7()), Owner, "Borrador", CreatedAt);
        course.AddLesson(Owner, new LessonId(Guid.CreateVersion7()), "Uno", "Descripcion",
            "https://videos.example.com/1.mp4");
        courses.Add(course);

        await Assert.ThrowsAsync<InvalidCourseStateException>(() => Handler(Owner).HandleAsync(
            new RepublishCourseCommand(course.Id),
            CancellationToken.None));

        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task ActorNoPropietario_LanzaYNoLlegaAConfirmar()
    {
        var course = SeedPublishedCourse();
        course.Rename(Owner, "Titulo nuevo");

        await Assert.ThrowsAsync<CourseOwnershipException>(() => Handler(Intruder).HandleAsync(
            new RepublishCourseCommand(course.Id),
            CancellationToken.None));

        Assert.Equal(0, unitOfWork.SaveChangesCount);
        Assert.Equal("Microservicios con .NET", course.PublishedTitle);
    }

    [Fact]
    public async Task CursoInexistente_DevuelveNullYNoConfirma()
    {
        var view = await Handler(Owner).HandleAsync(
            new RepublishCourseCommand(new CourseId(Guid.CreateVersion7())),
            CancellationToken.None);

        Assert.Null(view);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    private Course SeedPublishedCourse()
    {
        var course = Course.Create(
            new CourseId(Guid.CreateVersion7()), Owner, "Microservicios con .NET", CreatedAt);

        course.AddLesson(Owner, new LessonId(Guid.CreateVersion7()), "Uno", "Descripcion",
            "https://videos.example.com/1.mp4");
        course.Publish(Owner, PublishedAt);
        course.ClearDomainEvents();

        courses.Add(course);

        return course;
    }

    private RepublishCourseHandler Handler(InstructorId actor) =>
        new(courses, unitOfWork, new StubCurrentActor(actor), timeProvider);
}
