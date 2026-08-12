using CourseAuthoring.Domain.Courses;
using CourseAuthoring.Domain.Courses.Exceptions;

namespace CourseAuthoring.Domain.Tests;

public sealed class CourseOwnershipTests
{
    private static readonly InstructorId Owner = new(Guid.CreateVersion7());
    private static readonly InstructorId Intruder = new(Guid.CreateVersion7());
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);

    private static Course NewCourse() =>
        Course.Create(new CourseId(Guid.CreateVersion7()), Owner, "Microservicios con .NET", CreatedAt);

    private static LessonId AddLesson(Course course, string title) =>
        course.AddLesson(Owner, new LessonId(Guid.CreateVersion7()), title, $"Descripcion de {title}",
            "https://videos.example.com/leccion.mp4");

    [Fact]
    public void AddLesson_ConActorAjeno_LanzaYNoAnadeNada()
    {
        var course = NewCourse();

        Assert.Throws<CourseOwnershipException>(
            () => course.AddLesson(Intruder, new LessonId(Guid.CreateVersion7()), "Titulo", "Descripcion",
                "https://videos.example.com/leccion.mp4"));

        Assert.Empty(course.WorkingLessons);
    }

    [Fact]
    public void UpdateLesson_ConActorAjeno_LanzaYNoCambiaElContenido()
    {
        var course = NewCourse();
        var lessonId = AddLesson(course, "Uno");

        Assert.Throws<CourseOwnershipException>(
            () => course.UpdateLesson(Intruder, lessonId, "Secuestrada", "Otra",
                "https://videos.example.com/otra.mp4"));

        Assert.Equal("Uno", Assert.Single(course.WorkingLessons).Title);
    }

    [Fact]
    public void RemoveLesson_ConActorAjeno_LanzaYNoEliminaNada()
    {
        var course = NewCourse();
        var lessonId = AddLesson(course, "Uno");

        Assert.Throws<CourseOwnershipException>(() => course.RemoveLesson(Intruder, lessonId));

        Assert.Single(course.WorkingLessons);
    }

    [Fact]
    public void ReorderLessons_ConActorAjeno_LanzaYNoAlteraLasPosiciones()
    {
        var course = NewCourse();
        var uno = AddLesson(course, "Uno");
        var dos = AddLesson(course, "Dos");

        Assert.Throws<CourseOwnershipException>(() => course.ReorderLessons(Intruder, [dos, uno]));

        Assert.Equal(["Uno", "Dos"], course.WorkingLessons.Select(lesson => lesson.Title));
    }

    [Fact]
    public void Rename_ConActorAjeno_LanzaYNoCambiaElTitulo()
    {
        var course = NewCourse();

        Assert.Throws<CourseOwnershipException>(() => course.Rename(Intruder, "Secuestrado"));

        Assert.Equal("Microservicios con .NET", course.Title);
    }

    [Fact]
    public void CourseOwnershipException_LlevaElCursoYElActorRechazado()
    {
        var course = NewCourse();

        var exception = Assert.Throws<CourseOwnershipException>(() => course.Rename(Intruder, "Secuestrado"));

        Assert.Equal(course.Id, exception.CourseId);
        Assert.Equal(Intruder, exception.Actor);
    }
}
