using CourseAuthoring.Application.Courses.AddLesson;
using CourseAuthoring.Application.Tests.Fakes;
using CourseAuthoring.Domain.Courses;
using CourseAuthoring.Domain.Courses.Exceptions;

namespace CourseAuthoring.Application.Tests;

public sealed class AddLessonHandlerTests
{
    private static readonly InstructorId Owner = new(Guid.CreateVersion7());
    private static readonly InstructorId Intruder = new(Guid.CreateVersion7());
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryCourseRepository courses = new();
    private readonly NoOpUnitOfWork unitOfWork = new();

    [Fact]
    public async Task Anade_LaLeccionYConfirmaLaUnidadDeTrabajo()
    {
        var course = SeedCourse();

        var view = await Handler(Owner).HandleAsync(
            new AddLessonCommand(course.Id, "Introduccion", "Que es un microservicio",
                "https://videos.example.com/1.mp4"),
            CancellationToken.None);

        Assert.NotNull(view);
        Assert.Equal("Introduccion", view.Title);
        Assert.Equal(1, view.Position);
        Assert.NotEqual(Guid.Empty, view.Id);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task CursoInexistente_DevuelveNullYNoConfirma()
    {
        var view = await Handler(Owner).HandleAsync(
            new AddLessonCommand(new CourseId(Guid.CreateVersion7()), "Titulo", "Descripcion",
                "https://videos.example.com/1.mp4"),
            CancellationToken.None);

        Assert.Null(view);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task ActorNoPropietario_LanzaYNoLlegaAConfirmar()
    {
        var course = SeedCourse();

        await Assert.ThrowsAsync<CourseOwnershipException>(() => Handler(Intruder).HandleAsync(
            new AddLessonCommand(course.Id, "Titulo", "Descripcion",
                "https://videos.example.com/1.mp4"),
            CancellationToken.None));

        Assert.Equal(0, unitOfWork.SaveChangesCount);
        Assert.Empty(course.WorkingLessons);
    }

    [Fact]
    public async Task DatosInvalidos_LanzaYNoLlegaAConfirmar()
    {
        var course = SeedCourse();

        await Assert.ThrowsAsync<InvalidVideoUrlException>(() => Handler(Owner).HandleAsync(
            new AddLessonCommand(course.Id, "Titulo", "Descripcion", "/videos/relativa.mp4"),
            CancellationToken.None));

        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    private Course SeedCourse()
    {
        var course = Course.Create(
            new CourseId(Guid.CreateVersion7()), Owner, "Microservicios con .NET", CreatedAt);

        courses.Add(course);

        return course;
    }

    private AddLessonHandler Handler(InstructorId actor) =>
        new(courses, unitOfWork, new StubCurrentActor(actor));
}
