using CourseAuthoring.Application.Abstractions;
using CourseAuthoring.Domain.Courses;
using CourseAuthoring.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CourseAuthoring.Integration.Tests;

public sealed class CatalogQueriesTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Browse_NoIncluyeCursosEnBorrador()
    {
        await Reset();

        var borrador = NewCourse("Borrador");
        AddLesson(borrador, "Uno");
        await Persist(borrador);

        var publicado = await PublishedCourse("Publicado", PublishedAt, "Uno");

        var page = await Browse(1, 20);

        Assert.Equal(1, page.TotalCount);
        Assert.Equal(publicado.Value, Assert.Single(page.Items).Id);
    }

    [Fact]
    public async Task Browse_DevuelveElTituloPublicadoYElNumeroDeLeccionesPublicadas()
    {
        await Reset();

        var courseId = await PublishedCourse("Titulo publicado", PublishedAt, "Uno", "Dos");

        await Edit(courseId, course =>
        {
            course.Rename(Owner, "Titulo de trabajo");
            AddLesson(course, "Tres");
        });

        var summary = Assert.Single((await Browse(1, 20)).Items);

        Assert.Equal("Titulo publicado", summary.Title);
        Assert.Equal(2, summary.LessonCount);
        Assert.Equal(PublishedAt, summary.PublishedAt);
        Assert.Equal(PublishedAt, summary.PublishedContentUpdatedAt);
    }

    [Fact]
    public async Task Browse_OrdenaPorPublishedAtDescendente_YRepublicarNoAlteraLaPosicion()
    {
        await Reset();

        var antiguo = await PublishedCourse("Antiguo", PublishedAt, "Uno");
        var reciente = await PublishedCourse("Reciente", PublishedAt.AddHours(1), "Uno");

        Assert.Equal([reciente.Value, antiguo.Value], (await Browse(1, 20)).Items.Select(item => item.Id));

        await Edit(antiguo, course =>
        {
            course.Rename(Owner, "Antiguo republicado");
            Assert.True(course.Republish(Owner, RepublishedAt));
        });

        var afterRepublish = await Browse(1, 20);

        Assert.Equal([reciente.Value, antiguo.Value], afterRepublish.Items.Select(item => item.Id));
        Assert.Equal("Antiguo republicado", afterRepublish.Items[1].Title);
        Assert.Equal(RepublishedAt, afterRepublish.Items[1].PublishedContentUpdatedAt);
    }

    [Fact]
    public async Task Browse_PaginaConLosCuatroCampos()
    {
        await Reset();

        for (var index = 0; index < 3; index++)
        {
            await PublishedCourse($"Curso {index}", PublishedAt.AddMinutes(index), "Uno");
        }

        var page = await Browse(2, 2);

        Assert.Equal(2, page.Page);
        Assert.Equal(2, page.PageSize);
        Assert.Equal(3, page.TotalCount);
        Assert.Single(page.Items);
    }

    [Fact]
    public async Task GetPublishedCourse_DeUnBorrador_DevuelveNull()
    {
        await Reset();

        var borrador = NewCourse("Borrador");
        AddLesson(borrador, "Uno");
        await Persist(borrador);

        Assert.Null(await GetPublished(borrador.Id));
    }

    [Fact]
    public async Task GetPublishedCourse_DeUnCursoInexistente_DevuelveNull()
    {
        await Reset();

        Assert.Null(await GetPublished(new CourseId(Guid.CreateVersion7())));
    }

    [Fact]
    public async Task GetPublishedCourse_DevuelveElSnapshotYNoElContenidoDeTrabajo()
    {
        await Reset();

        var courseId = await PublishedCourse("Titulo publicado", PublishedAt, "Uno", "Dos");

        await Edit(courseId, course =>
        {
            course.Rename(Owner, "Titulo de trabajo");
            course.UpdateLesson(Owner, course.WorkingLessons[0].Id, "Uno corregida", "Otra descripcion",
                "https://cdn.example.com/v2.mp4");
            AddLesson(course, "Tres");
        });

        var detail = await GetPublished(courseId);

        Assert.NotNull(detail);
        Assert.Equal("Titulo publicado", detail.Title);
        Assert.Equal(Owner.Value, detail.InstructorId);
        Assert.Equal(["Uno", "Dos"], detail.Lessons.Select(lesson => lesson.Title));
        Assert.Equal([1, 2], detail.Lessons.Select(lesson => lesson.Position));

        await Edit(courseId, course => Assert.True(course.Republish(Owner, RepublishedAt)));

        var afterRepublish = await GetPublished(courseId);

        Assert.NotNull(afterRepublish);
        Assert.Equal("Titulo de trabajo", afterRepublish.Title);
        Assert.Equal(["Uno corregida", "Dos", "Tres"], afterRepublish.Lessons.Select(lesson => lesson.Title));
        Assert.Equal(RepublishedAt, afterRepublish.PublishedContentUpdatedAt);
        Assert.Equal(PublishedAt, afterRepublish.PublishedAt);
    }

    private static readonly InstructorId Owner = new(Guid.CreateVersion7());
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PublishedAt = new(2026, 8, 12, 13, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset RepublishedAt = new(2026, 8, 12, 14, 0, 0, TimeSpan.Zero);

    private static Course NewCourse(string title) =>
        Course.Create(new CourseId(Guid.CreateVersion7()), Owner, title, CreatedAt);

    private static LessonId AddLesson(Course course, string title) =>
        course.AddLesson(Owner, new LessonId(Guid.CreateVersion7()), title, $"Descripcion de {title}",
            "https://videos.example.com/leccion.mp4");

    private async Task Reset()
    {
        await using var context = fixture.CreateContext();

        await context.Courses.ExecuteDeleteAsync(CancellationToken.None);
    }

    private async Task Persist(Course course)
    {
        await using var context = fixture.CreateContext();

        context.Courses.Add(course);
        await context.SaveChangesAsync(CancellationToken.None);
    }

    private async Task<CourseId> PublishedCourse(
        string title,
        DateTimeOffset publishedAt,
        params string[] lessonTitles)
    {
        var course = NewCourse(title);

        foreach (var lessonTitle in lessonTitles)
        {
            AddLesson(course, lessonTitle);
        }

        course.Publish(Owner, publishedAt);
        await Persist(course);

        return course.Id;
    }

    private async Task Edit(CourseId courseId, Action<Course> edit)
    {
        await using var context = fixture.CreateContext();

        var course = await context.Courses
            .Include(candidate => candidate.WorkingLessons.OrderBy(lesson => lesson.Position))
            .Include(candidate => candidate.PublishedLessons.OrderBy(lesson => lesson.Position))
            .FirstOrDefaultAsync(candidate => candidate.Id == courseId, CancellationToken.None);

        Assert.NotNull(course);

        edit(course);

        await context.SaveChangesAsync(CancellationToken.None);
    }

    private async Task<Application.Common.PagedResult<Application.Catalog.CatalogCourseSummaryView>>
        Browse(int page, int pageSize)
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        return await scope.ServiceProvider
            .GetRequiredService<ICatalogQueries>()
            .BrowseAsync(page, pageSize, CancellationToken.None);
    }

    private async Task<Application.Catalog.CatalogCourseView?> GetPublished(CourseId courseId)
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        return await scope.ServiceProvider
            .GetRequiredService<ICatalogQueries>()
            .GetPublishedCourseAsync(courseId, CancellationToken.None);
    }

    private ServiceProvider BuildProvider() =>
        new ServiceCollection()
            .AddInfrastructure(fixture.ConnectionString)
            .BuildServiceProvider();
}
