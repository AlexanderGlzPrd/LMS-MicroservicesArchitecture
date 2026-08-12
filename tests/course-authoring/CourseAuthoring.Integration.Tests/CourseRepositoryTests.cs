using CourseAuthoring.Domain.Courses;
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
}
