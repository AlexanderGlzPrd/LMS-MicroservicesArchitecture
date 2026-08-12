using CourseAuthoring.Domain.Courses;
using CourseAuthoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CourseAuthoring.Integration.Tests;

public sealed class CourseRepositoryTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Course_SePersisteYSeReleeConTodosSusCampos()
    {
        var id = new CourseId(Guid.CreateVersion7());
        var instructorId = new InstructorId(Guid.CreateVersion7());
        var createdAt = new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
        var course = Course.Create(id, instructorId, "Microservicios con .NET 10", createdAt);

        await using (var writeContext = fixture.CreateContext())
        {
            writeContext.Courses.Add(course);
            await writeContext.SaveChangesAsync(CancellationToken.None);
        }

        await using var readContext = fixture.CreateContext();
        var persisted = await readContext.Courses
            .SingleOrDefaultAsync(c => c.Id == id, CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Equal(id, persisted.Id);
        Assert.Equal(instructorId, persisted.InstructorId);
        Assert.Equal("Microservicios con .NET 10", persisted.Title);
        Assert.Equal(CourseStatus.Draft, persisted.Status);
        Assert.Equal(createdAt, persisted.CreatedAt);
    }

    [Fact]
    public async Task Course_InexistenteDevuelveNull()
    {
        var missingId = new CourseId(Guid.CreateVersion7());

        await using var context = fixture.CreateContext();

        var missing = await context.Courses
            .SingleOrDefaultAsync(c => c.Id == missingId, CancellationToken.None);

        Assert.Null(missing);
    }

    [Fact]
    public async Task ContenidoDeTrabajo_SePersisteYSeReleeOrdenadoPorPosicion()
    {
        var course = NewCourse();
        AddLesson(course, "Uno");
        AddLesson(course, "Dos");
        AddLesson(course, "Tres");

        await Persist(course);

        var persisted = await LoadAggregate(course.Id);

        Assert.Equal(["Uno", "Dos", "Tres"], persisted.WorkingLessons.Select(lesson => lesson.Title));
        Assert.Equal([1, 2, 3], persisted.WorkingLessons.Select(lesson => lesson.Position));
        Assert.Empty(persisted.PublishedLessons);
    }

    [Fact]
    public async Task Publish_PersisteElSnapshotConLosMismosLessonId()
    {
        var course = NewCourse();
        AddLesson(course, "Uno");
        AddLesson(course, "Dos");
        course.Publish(Owner, PublishedAt);

        await Persist(course);

        var persisted = await LoadAggregate(course.Id);

        Assert.Equal(CourseStatus.Published, persisted.Status);
        Assert.Equal("Microservicios con .NET", persisted.PublishedTitle);
        Assert.Equal(PublishedAt, persisted.PublishedAt);
        Assert.Equal(PublishedAt, persisted.PublishedContentUpdatedAt);
        Assert.Equal(["Uno", "Dos"], persisted.PublishedLessons.Select(lesson => lesson.Title));
        Assert.Equal(
            persisted.WorkingLessons.Select(lesson => lesson.Id),
            persisted.PublishedLessons.Select(lesson => lesson.Id));
    }

    // El caso que rompe si el snapshot se borra entero y se reinserta entero:
    // EF Core rastrearia el mismo LessonId como Deleted y como Added en la misma unidad de trabajo.
    [Fact]
    public async Task Republish_TrasEditarAnadirYEliminar_ReconciliaElSnapshotSinFallar()
    {
        var course = NewCourse();
        AddLesson(course, "Uno");
        AddLesson(course, "Dos");
        AddLesson(course, "Tres");
        course.Publish(Owner, PublishedAt);
        await Persist(course);

        LessonId conservada;
        LessonId editada;

        await using (var writeContext = fixture.CreateContext())
        {
            var tracked = await LoadAggregate(writeContext, course.Id);

            conservada = tracked.WorkingLessons[0].Id;
            editada = tracked.WorkingLessons[1].Id;

            tracked.Rename(Owner, "Titulo republicado");
            tracked.UpdateLesson(Owner, editada, "Dos corregida", "Otra descripcion",
                "https://cdn.example.com/v2.mp4");
            tracked.RemoveLesson(Owner, tracked.WorkingLessons[2].Id);
            AddLesson(tracked, "Cuatro");

            Assert.True(tracked.Republish(Owner, RepublishedAt));

            await writeContext.SaveChangesAsync(CancellationToken.None);
        }

        var persisted = await LoadAggregate(course.Id);

        Assert.Equal("Titulo republicado", persisted.PublishedTitle);
        Assert.Equal(PublishedAt, persisted.PublishedAt);
        Assert.Equal(RepublishedAt, persisted.PublishedContentUpdatedAt);

        Assert.Equal(["Uno", "Dos corregida", "Cuatro"],
            persisted.PublishedLessons.Select(lesson => lesson.Title));
        Assert.Equal([1, 2, 3], persisted.PublishedLessons.Select(lesson => lesson.Position));

        // Las lecciones que sobreviven conservan su LessonId: se actualizan, no se recrean.
        Assert.Equal(conservada, persisted.PublishedLessons[0].Id);
        Assert.Equal(editada, persisted.PublishedLessons[1].Id);
        Assert.Equal("https://cdn.example.com/v2.mp4", persisted.PublishedLessons[1].VideoUrl);

        Assert.Equal(
            persisted.WorkingLessons.Select(lesson => lesson.Id),
            persisted.PublishedLessons.Select(lesson => lesson.Id));
    }

    [Fact]
    public async Task EditarContenidoDeTrabajo_SinRepublicar_NoTocaLasFilasPublicadas()
    {
        var course = NewCourse();
        AddLesson(course, "Uno");
        course.Publish(Owner, PublishedAt);
        await Persist(course);

        await using (var writeContext = fixture.CreateContext())
        {
            var tracked = await LoadAggregate(writeContext, course.Id);

            tracked.Rename(Owner, "Titulo nuevo");
            tracked.UpdateLesson(Owner, tracked.WorkingLessons[0].Id, "Uno corregida", "Otra descripcion",
                "https://cdn.example.com/v2.mp4");

            await writeContext.SaveChangesAsync(CancellationToken.None);
        }

        var persisted = await LoadAggregate(course.Id);

        Assert.Equal("Titulo nuevo", persisted.Title);
        Assert.Equal("Microservicios con .NET", persisted.PublishedTitle);
        Assert.Equal("Uno", Assert.Single(persisted.PublishedLessons).Title);
        Assert.Equal(PublishedAt, persisted.PublishedContentUpdatedAt);
    }

    private static readonly InstructorId Owner = new(Guid.CreateVersion7());
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PublishedAt = new(2026, 8, 12, 13, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset RepublishedAt = new(2026, 8, 12, 14, 0, 0, TimeSpan.Zero);

    private static Course NewCourse() =>
        Course.Create(new CourseId(Guid.CreateVersion7()), Owner, "Microservicios con .NET", CreatedAt);

    private static LessonId AddLesson(Course course, string title) =>
        course.AddLesson(Owner, new LessonId(Guid.CreateVersion7()), title, $"Descripcion de {title}",
            "https://videos.example.com/leccion.mp4");

    private async Task Persist(Course course)
    {
        await using var context = fixture.CreateContext();

        context.Courses.Add(course);
        await context.SaveChangesAsync(CancellationToken.None);
    }

    private async Task<Course> LoadAggregate(CourseId id)
    {
        await using var context = fixture.CreateContext();

        return await LoadAggregate(context, id);
    }

    private static async Task<Course> LoadAggregate(CourseAuthoringDbContext context, CourseId id)
    {
        var course = await context.Courses
            .Include(c => c.WorkingLessons.OrderBy(lesson => lesson.Position))
            .Include(c => c.PublishedLessons.OrderBy(lesson => lesson.Position))
            .FirstOrDefaultAsync(c => c.Id == id, CancellationToken.None);

        Assert.NotNull(course);

        return course;
    }
}
