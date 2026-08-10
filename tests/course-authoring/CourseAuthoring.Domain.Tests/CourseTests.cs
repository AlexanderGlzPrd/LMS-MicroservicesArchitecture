using CourseAuthoring.Domain.Courses;

namespace CourseAuthoring.Domain.Tests;

public sealed class CourseTests
{
    private static readonly CourseId Id = new(Guid.CreateVersion7());
    private static readonly InstructorId Instructor = new(Guid.CreateVersion7());
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ConTituloValido_DejaElCursoEnDraft()
    {
        var course = Course.Create(Id, Instructor, "Microservicios con .NET", CreatedAt);

        Assert.Equal(CourseStatus.Draft, course.Status);
    }

    [Fact]
    public void Create_ConTituloValido_ConservaLosDatosRecibidos()
    {
        var course = Course.Create(Id, Instructor, "Curso Microservicios con .NET 10", CreatedAt);

        Assert.Equal(Id, course.Id);
        Assert.Equal(Instructor, course.InstructorId);
        Assert.Equal("Curso Microservicios con .NET 10", course.Title);
        Assert.Equal(CreatedAt, course.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Create_ConTituloVacio_LanzaExcepcionDeDominio(string title)
    {
        Assert.Throws<InvalidCourseTitleException>(
            () => Course.Create(Id, Instructor, title, CreatedAt));
    }
}
