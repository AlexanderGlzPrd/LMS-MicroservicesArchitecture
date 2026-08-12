using CourseAuthoring.Domain.Courses;
using CourseAuthoring.Domain.Courses.Events;
using CourseAuthoring.Domain.Courses.Exceptions;

namespace CourseAuthoring.Domain.Tests;

public sealed class CoursePublishingTests
{
    private static readonly InstructorId Owner = new(Guid.CreateVersion7());
    private static readonly InstructorId Intruder = new(Guid.CreateVersion7());
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PublishedAt = new(2026, 8, 12, 13, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset RepublishedAt = new(2026, 8, 12, 14, 0, 0, TimeSpan.Zero);

    private static Course NewCourse() =>
        Course.Create(new CourseId(Guid.CreateVersion7()), Owner, "Microservicios con .NET", CreatedAt);

    private static LessonId AddLesson(Course course, string title) =>
        course.AddLesson(Owner, new LessonId(Guid.CreateVersion7()), title, $"Descripcion de {title}",
            "https://videos.example.com/leccion.mp4");

    private static Course PublishedCourse(params string[] lessonTitles)
    {
        var course = NewCourse();

        foreach (var title in lessonTitles)
        {
            AddLesson(course, title);
        }

        course.Publish(Owner, PublishedAt);
        course.ClearDomainEvents();

        return course;
    }

    [Fact]
    public void Publish_SinLecciones_LanzaCourseHasNoLessons()
    {
        var course = NewCourse();

        Assert.Throws<CourseHasNoLessonsException>(() => course.Publish(Owner, PublishedAt));
        Assert.Equal(CourseStatus.Draft, course.Status);
        Assert.Null(course.PublishedAt);
    }

    [Fact]
    public void Publish_ConLecciones_DejaElCursoPublicadoYFijaLasDosFechas()
    {
        var course = NewCourse();
        AddLesson(course, "Uno");

        course.Publish(Owner, PublishedAt);

        Assert.Equal(CourseStatus.Published, course.Status);
        Assert.Equal(PublishedAt, course.PublishedAt);
        Assert.Equal(PublishedAt, course.PublishedContentUpdatedAt);
        Assert.Equal("Microservicios con .NET", course.PublishedTitle);
    }

    [Fact]
    public void Publish_CopiaElContenidoDeTrabajoAlSnapshot()
    {
        var course = PublishedCourse("Uno", "Dos");

        Assert.Equal(["Uno", "Dos"], course.PublishedLessons.Select(lesson => lesson.Title));
        Assert.Equal([1, 2], course.PublishedLessons.Select(lesson => lesson.Position));
    }

    [Fact]
    public void Publish_ConservaElLessonIdDeLaLeccionDeTrabajo()
    {
        var course = PublishedCourse("Uno", "Dos");

        Assert.Equal(
            course.WorkingLessons.Select(lesson => lesson.Id),
            course.PublishedLessons.Select(lesson => lesson.Id));
    }

    [Fact]
    public void Publish_RegistraCoursePublishedExactamenteUnaVez()
    {
        var course = NewCourse();
        AddLesson(course, "Uno");

        course.Publish(Owner, PublishedAt);

        var published = Assert.IsType<CoursePublished>(Assert.Single(course.DomainEvents));
        Assert.Equal(course.Id, published.CourseId);
        Assert.Equal(Owner, published.InstructorId);
        Assert.Equal(PublishedAt, published.OccurredAt);
    }

    [Fact]
    public void Publish_SobreCursoYaPublicado_LanzaInvalidCourseState()
    {
        var course = PublishedCourse("Uno");

        Assert.Throws<InvalidCourseStateException>(() => course.Publish(Owner, RepublishedAt));
    }

    [Fact]
    public void Publish_ConActorAjeno_LanzaYNoPublica()
    {
        var course = NewCourse();
        AddLesson(course, "Uno");

        Assert.Throws<CourseOwnershipException>(() => course.Publish(Intruder, PublishedAt));
        Assert.Equal(CourseStatus.Draft, course.Status);
        Assert.Empty(course.DomainEvents);
    }

    [Fact]
    public void Republish_SobreBorrador_LanzaInvalidCourseState()
    {
        var course = NewCourse();
        AddLesson(course, "Uno");

        Assert.Throws<InvalidCourseStateException>(() => course.Republish(Owner, RepublishedAt));
    }

    [Fact]
    public void Republish_SinLeccionesDeTrabajo_LanzaCourseHasNoLessons()
    {
        var course = PublishedCourse("Unica");
        course.RemoveLesson(Owner, course.WorkingLessons[0].Id);

        Assert.Throws<CourseHasNoLessonsException>(() => course.Republish(Owner, RepublishedAt));
    }

    [Fact]
    public void RemoveLesson_EnCursoPublicado_DejaElContenidoDeTrabajoVacioYNoTocaElSnapshot()
    {
        var course = PublishedCourse("Unica");

        course.RemoveLesson(Owner, course.WorkingLessons[0].Id);

        Assert.Empty(course.WorkingLessons);
        Assert.Single(course.PublishedLessons);
    }

    [Fact]
    public void Republish_SinCambios_DevuelveFalseSinEventoYSinTocarLasFechas()
    {
        var course = PublishedCourse("Uno", "Dos");

        var changed = course.Republish(Owner, RepublishedAt);

        Assert.False(changed);
        Assert.Empty(course.DomainEvents);
        Assert.Equal(PublishedAt, course.PublishedAt);
        Assert.Equal(PublishedAt, course.PublishedContentUpdatedAt);
    }

    [Fact]
    public void EditarContenidoDeTrabajo_NoModificaElSnapshotHastaRepublicar()
    {
        var course = PublishedCourse("Uno");
        var lessonId = course.WorkingLessons[0].Id;

        course.Rename(Owner, "Titulo nuevo");
        course.UpdateLesson(Owner, lessonId, "Uno corregida", "Otra descripcion",
            "https://cdn.example.com/v2.mp4");
        AddLesson(course, "Dos");

        Assert.Equal("Microservicios con .NET", course.PublishedTitle);
        Assert.Equal("Uno", Assert.Single(course.PublishedLessons).Title);
        Assert.Equal(PublishedAt, course.PublishedContentUpdatedAt);
        Assert.Empty(course.DomainEvents);
    }

    [Fact]
    public void Republish_ConCambios_DevuelveTrueReemplazaElSnapshotYRegistraEvento()
    {
        var course = PublishedCourse("Uno");
        course.Rename(Owner, "Titulo nuevo");
        AddLesson(course, "Dos");

        var changed = course.Republish(Owner, RepublishedAt);

        Assert.True(changed);
        Assert.Equal("Titulo nuevo", course.PublishedTitle);
        Assert.Equal(["Uno", "Dos"], course.PublishedLessons.Select(lesson => lesson.Title));

        var modified = Assert.IsType<PublishedContentModified>(Assert.Single(course.DomainEvents));
        Assert.Equal(course.Id, modified.CourseId);
        Assert.Equal(RepublishedAt, modified.OccurredAt);
    }

    [Fact]
    public void Republish_ConCambios_ActualizaContentUpdatedAtYNoPublishedAt()
    {
        var course = PublishedCourse("Uno");
        course.Rename(Owner, "Titulo nuevo");

        course.Republish(Owner, RepublishedAt);

        Assert.Equal(PublishedAt, course.PublishedAt);
        Assert.Equal(RepublishedAt, course.PublishedContentUpdatedAt);
    }

    [Fact]
    public void Republish_SoloConReordenamiento_DetectaElCambio()
    {
        var course = PublishedCourse("Uno", "Dos");
        var ids = course.WorkingLessons.Select(lesson => lesson.Id).ToList();

        course.ReorderLessons(Owner, [ids[1], ids[0]]);

        Assert.True(course.Republish(Owner, RepublishedAt));
        Assert.Equal(["Dos", "Uno"], course.PublishedLessons.Select(lesson => lesson.Title));
    }

    [Fact]
    public void Republish_TrasEditarAnadirYEliminar_DejaLaSecuenciaDeTrabajoExacta()
    {
        var course = PublishedCourse("Uno", "Dos", "Tres");
        var conservada = course.WorkingLessons[0].Id;
        var editada = course.WorkingLessons[1].Id;

        course.UpdateLesson(Owner, editada, "Dos corregida", "Otra descripcion",
            "https://cdn.example.com/v2.mp4");
        course.RemoveLesson(Owner, course.WorkingLessons[2].Id);
        AddLesson(course, "Cuatro");

        Assert.True(course.Republish(Owner, RepublishedAt));

        Assert.Equal(["Uno", "Dos corregida", "Cuatro"],
            course.PublishedLessons.Select(lesson => lesson.Title));
        Assert.Equal([1, 2, 3], course.PublishedLessons.Select(lesson => lesson.Position));
        Assert.Equal(conservada, course.PublishedLessons[0].Id);
        Assert.Equal(editada, course.PublishedLessons[1].Id);
    }

    [Fact]
    public void Republish_ConActorAjeno_LanzaYNoTocaElSnapshot()
    {
        var course = PublishedCourse("Uno");
        course.Rename(Owner, "Titulo nuevo");

        Assert.Throws<CourseOwnershipException>(() => course.Republish(Intruder, RepublishedAt));
        Assert.Equal("Microservicios con .NET", course.PublishedTitle);
    }

    [Fact]
    public void ClearDomainEvents_VaciaElRegistro()
    {
        var course = NewCourse();
        AddLesson(course, "Uno");
        course.Publish(Owner, PublishedAt);

        course.ClearDomainEvents();

        Assert.Empty(course.DomainEvents);
    }
}
