using CourseAuthoring.Domain.Courses;
using CourseAuthoring.Domain.Courses.Exceptions;

namespace CourseAuthoring.Domain.Tests;

public sealed class CourseLessonTests
{
    private static readonly InstructorId Owner = new(Guid.CreateVersion7());
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);

    private static Course NewCourse() =>
        Course.Create(new CourseId(Guid.CreateVersion7()), Owner, "Microservicios con .NET", CreatedAt);

    private static LessonId AddLesson(Course course, string title) =>
        course.AddLesson(Owner, new LessonId(Guid.CreateVersion7()), title, $"Descripcion de {title}",
            "https://videos.example.com/leccion.mp4");

    [Fact]
    public void AddLesson_SobreCursoVacio_AsignaPosicionUno()
    {
        var course = NewCourse();

        AddLesson(course, "Introduccion");

        Assert.Equal(1, Assert.Single(course.WorkingLessons).Position);
    }

    [Fact]
    public void AddLesson_TresVeces_AsignaPosicionesContiguas()
    {
        var course = NewCourse();

        AddLesson(course, "Uno");
        AddLesson(course, "Dos");
        AddLesson(course, "Tres");

        Assert.Equal([1, 2, 3], course.WorkingLessons.Select(lesson => lesson.Position));
    }

    [Fact]
    public void RemoveLesson_DeLaSegundaDeTres_RecompactaAUnoYDos()
    {
        var course = NewCourse();
        AddLesson(course, "Uno");
        var segunda = AddLesson(course, "Dos");
        AddLesson(course, "Tres");

        course.RemoveLesson(Owner, segunda);

        Assert.Equal([1, 2], course.WorkingLessons.Select(lesson => lesson.Position));
        Assert.Equal(["Uno", "Tres"], course.WorkingLessons.Select(lesson => lesson.Title));
    }

    [Fact]
    public void RemoveLesson_DeTodasLasLecciones_DejaElContenidoDeTrabajoVacio()
    {
        var course = NewCourse();
        var unica = AddLesson(course, "Unica");

        course.RemoveLesson(Owner, unica);

        Assert.Empty(course.WorkingLessons);
    }

    [Fact]
    public void RemoveLesson_ConIdentificadorAjeno_LanzaLessonNotFound()
    {
        var course = NewCourse();
        AddLesson(course, "Uno");

        Assert.Throws<LessonNotFoundException>(
            () => course.RemoveLesson(Owner, new LessonId(Guid.CreateVersion7())));
    }

    [Fact]
    public void UpdateLesson_ConDatosValidos_CambiaElContenidoYNoLaPosicion()
    {
        var course = NewCourse();
        AddLesson(course, "Uno");
        var segunda = AddLesson(course, "Dos");

        course.UpdateLesson(Owner, segunda, "Dos corregida", "Nueva descripcion",
            "https://cdn.example.com/v2.mp4");

        var lesson = course.WorkingLessons.Single(candidate => candidate.Id == segunda);
        Assert.Equal("Dos corregida", lesson.Title);
        Assert.Equal("Nueva descripcion", lesson.Description);
        Assert.Equal("https://cdn.example.com/v2.mp4", lesson.VideoUrl);
        Assert.Equal(2, lesson.Position);
    }

    [Fact]
    public void ReorderLessons_ConPermutacionExacta_ReasignaLasPosiciones()
    {
        var course = NewCourse();
        var uno = AddLesson(course, "Uno");
        var dos = AddLesson(course, "Dos");
        var tres = AddLesson(course, "Tres");

        course.ReorderLessons(Owner, [tres, uno, dos]);

        Assert.Equal(["Tres", "Uno", "Dos"], course.WorkingLessons.Select(lesson => lesson.Title));
        Assert.Equal([1, 2, 3], course.WorkingLessons.Select(lesson => lesson.Position));
    }

    [Fact]
    public void ReorderLessons_ConListaIncompleta_LanzaYNoAlteraLasPosiciones()
    {
        var course = NewCourse();
        var uno = AddLesson(course, "Uno");
        AddLesson(course, "Dos");

        Assert.Throws<InvalidLessonOrderException>(() => course.ReorderLessons(Owner, [uno]));

        Assert.Equal(["Uno", "Dos"], course.WorkingLessons.Select(lesson => lesson.Title));
        Assert.Equal([1, 2], course.WorkingLessons.Select(lesson => lesson.Position));
    }

    [Fact]
    public void ReorderLessons_ConIdentificadorRepetido_LanzaYNoAlteraLasPosiciones()
    {
        var course = NewCourse();
        var uno = AddLesson(course, "Uno");
        AddLesson(course, "Dos");

        Assert.Throws<InvalidLessonOrderException>(() => course.ReorderLessons(Owner, [uno, uno]));

        Assert.Equal(["Uno", "Dos"], course.WorkingLessons.Select(lesson => lesson.Title));
        Assert.Equal([1, 2], course.WorkingLessons.Select(lesson => lesson.Position));
    }

    [Fact]
    public void ReorderLessons_ConIdentificadorAjeno_LanzaYNoAlteraLasPosiciones()
    {
        var course = NewCourse();
        var uno = AddLesson(course, "Uno");
        AddLesson(course, "Dos");

        Assert.Throws<InvalidLessonOrderException>(
            () => course.ReorderLessons(Owner, [uno, new LessonId(Guid.CreateVersion7())]));

        Assert.Equal(["Uno", "Dos"], course.WorkingLessons.Select(lesson => lesson.Title));
        Assert.Equal([1, 2], course.WorkingLessons.Select(lesson => lesson.Position));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddLesson_ConTituloVacio_LanzaInvalidLessonTitle(string title)
    {
        var course = NewCourse();

        Assert.Throws<InvalidLessonTitleException>(
            () => course.AddLesson(Owner, new LessonId(Guid.CreateVersion7()), title, "Descripcion",
                "https://videos.example.com/leccion.mp4"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddLesson_ConDescripcionVacia_LanzaInvalidLessonDescription(string description)
    {
        var course = NewCourse();

        Assert.Throws<InvalidLessonDescriptionException>(
            () => course.AddLesson(Owner, new LessonId(Guid.CreateVersion7()), "Titulo", description,
                "https://videos.example.com/leccion.mp4"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/videos/leccion.mp4")]
    [InlineData("videos.example.com/leccion.mp4")]
    [InlineData("ftp://videos.example.com/leccion.mp4")]
    [InlineData("javascript:alert(1)")]
    public void AddLesson_ConUrlNoAbsolutaHttp_LanzaInvalidVideoUrl(string videoUrl)
    {
        var course = NewCourse();

        Assert.Throws<InvalidVideoUrlException>(
            () => course.AddLesson(Owner, new LessonId(Guid.CreateVersion7()), "Titulo", "Descripcion", videoUrl));
    }

    [Theory]
    [InlineData("http://videos.example.com/leccion.mp4")]
    [InlineData("https://videos.example.com/leccion.mp4")]
    public void AddLesson_ConUrlAbsolutaHttp_EsAceptada(string videoUrl)
    {
        var course = NewCourse();

        course.AddLesson(Owner, new LessonId(Guid.CreateVersion7()), "Titulo", "Descripcion", videoUrl);

        Assert.Equal(videoUrl, Assert.Single(course.WorkingLessons).VideoUrl);
    }

    [Fact]
    public void AddLesson_NoImponeLongitudMaxima()
    {
        var course = NewCourse();
        var tituloLargo = new string('a', 500);

        course.AddLesson(Owner, new LessonId(Guid.CreateVersion7()), tituloLargo, new string('b', 5_000),
            "https://videos.example.com/" + new string('c', 3_000));

        Assert.Equal(tituloLargo, Assert.Single(course.WorkingLessons).Title);
    }

    [Fact]
    public void Rename_ConTituloValido_CambiaElTituloDeTrabajo()
    {
        var course = NewCourse();

        course.Rename(Owner, "Microservicios con .NET 10");

        Assert.Equal("Microservicios con .NET 10", course.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_ConTituloVacio_LanzaInvalidCourseTitle(string title)
    {
        var course = NewCourse();

        Assert.Throws<InvalidCourseTitleException>(() => course.Rename(Owner, title));
        Assert.Equal("Microservicios con .NET", course.Title);
    }
}
