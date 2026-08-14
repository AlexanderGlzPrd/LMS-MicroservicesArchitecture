using Learning.Application.Abstractions.Exceptions;
using Learning.Domain.Progress;
using Learning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace Learning.Integration.Tests;

public sealed class CourseProgressRepositoryTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly DateTimeOffset StartedAt = new(2026, 8, 14, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FirstMark = new(2026, 8, 14, 11, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SecondMark = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Progreso_SePersisteYSeReleeConSuColeccionHija()
    {
        var student = new StudentId(Guid.CreateVersion7());
        var course = new CourseId(Guid.CreateVersion7());
        var first = new LessonId(Guid.CreateVersion7());
        var second = new LessonId(Guid.CreateVersion7());

        await using (var writeContext = fixture.CreateContext())
        {
            var progress = CourseProgress.Start(student, course, StartedAt);
            progress.MarkLessonCompleted(first, Publicadas(first, second), FirstMark);
            progress.MarkLessonCompleted(second, Publicadas(first, second), SecondMark);

            writeContext.CourseProgresses.Add(progress);
            await writeContext.SaveChangesAsync(CancellationToken.None);
        }

        await using var readContext = fixture.CreateContext();
        var persisted = await FindAsync(readContext, student, course);

        Assert.NotNull(persisted);
        Assert.Equal(CourseProgressStatus.Completed, persisted.Status);
        Assert.Equal(StartedAt, persisted.StartedAt);
        Assert.Equal(SecondMark, persisted.CompletedAt);
        Assert.Equal(2, persisted.CompletedLessons.Count);
        Assert.Equal(
            [FirstMark, SecondMark],
            persisted.CompletedLessons.OrderBy(lesson => lesson.CompletedAt)
                .Select(lesson => lesson.CompletedAt));
    }

    [Fact]
    public async Task ParejaRepetida_LaRechazaLaClavePrimariaDelProgreso()
    {
        var student = new StudentId(Guid.CreateVersion7());
        var course = new CourseId(Guid.CreateVersion7());
        var lesson = new LessonId(Guid.CreateVersion7());

        await using (var firstContext = fixture.CreateContext())
        {
            firstContext.CourseProgresses.Add(CourseProgress.Start(student, course, StartedAt));
            await firstContext.SaveChangesAsync(CancellationToken.None);
        }

        await using var secondContext = fixture.CreateContext();
        var duplicate = CourseProgress.Start(student, course, StartedAt);
        duplicate.MarkLessonCompleted(lesson, Publicadas(lesson), FirstMark);
        secondContext.CourseProgresses.Add(duplicate);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => secondContext.SaveChangesAsync(CancellationToken.None));

        Assert.Equal(
            PostgresErrorCodes.UniqueViolation,
            (exception.InnerException as PostgresException)?.SqlState);
        Assert.Equal("pk_course_progress", (exception.InnerException as PostgresException)?.ConstraintName);
    }

    [Fact]
    public async Task TripletaRepetida_LaRechazaLaClavePrimariaDeLaLeccionCompletada()
    {
        var student = new StudentId(Guid.CreateVersion7());
        var course = new CourseId(Guid.CreateVersion7());
        var lesson = new LessonId(Guid.CreateVersion7());

        await using (var writeContext = fixture.CreateContext())
        {
            var progress = CourseProgress.Start(student, course, StartedAt);
            progress.MarkLessonCompleted(lesson, Publicadas(lesson), FirstMark);

            writeContext.CourseProgresses.Add(progress);
            await writeContext.SaveChangesAsync(CancellationToken.None);
        }

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync(CancellationToken.None);

        await using var command = new NpgsqlCommand(
            "INSERT INTO completed_lessons (student_id, course_id, lesson_id, completed_at) "
            + "VALUES (@student, @course, @lesson, @completedAt)",
            connection);

        command.Parameters.AddWithValue("student", student.Value);
        command.Parameters.AddWithValue("course", course.Value);
        command.Parameters.AddWithValue("lesson", lesson.Value);
        command.Parameters.AddWithValue("completedAt", SecondMark);

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync(CancellationToken.None));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, exception.SqlState);
        Assert.Equal("pk_completed_lessons", exception.ConstraintName);
    }

    [Fact]
    public async Task UnitOfWork_TraduceLaColisionDeLasDosClavesPrimariasYLimpiaElRastreador()
    {
        var student = new StudentId(Guid.CreateVersion7());
        var course = new CourseId(Guid.CreateVersion7());

        await using (var writeContext = fixture.CreateContext())
        {
            writeContext.CourseProgresses.Add(CourseProgress.Start(student, course, StartedAt));
            await writeContext.SaveChangesAsync(CancellationToken.None);
        }

        await using var context = fixture.CreateContext();
        context.CourseProgresses.Add(CourseProgress.Start(student, course, StartedAt));

        var unitOfWork = CreateUnitOfWork(context);

        await Assert.ThrowsAsync<ConcurrentCourseProgressException>(
            () => unitOfWork.SaveChangesAsync(CancellationToken.None));

        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task UnitOfWork_ConOtraViolacionDeUnicidad_LaPropagaSinLimpiarElRastreador()
    {
        const string CrearIndice =
            "CREATE UNIQUE INDEX ix_prueba_started_at ON course_progress (started_at) "
            + "WHERE started_at = TIMESTAMPTZ '2026-01-01 00:00:00+00'";

        const string BorrarIndice = "DROP INDEX IF EXISTS ix_prueba_started_at";

        var ajena = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        await using var context = fixture.CreateContext();
        await context.Database.ExecuteSqlRawAsync(CrearIndice, CancellationToken.None);

        try
        {
            await using (var writeContext = fixture.CreateContext())
            {
                writeContext.CourseProgresses.Add(CourseProgress.Start(
                    new StudentId(Guid.CreateVersion7()), new CourseId(Guid.CreateVersion7()), ajena));

                await writeContext.SaveChangesAsync(CancellationToken.None);
            }

            context.CourseProgresses.Add(CourseProgress.Start(
                new StudentId(Guid.CreateVersion7()), new CourseId(Guid.CreateVersion7()), ajena));

            var unitOfWork = CreateUnitOfWork(context);

            var exception = await Assert.ThrowsAsync<DbUpdateException>(
                () => unitOfWork.SaveChangesAsync(CancellationToken.None));

            var postgres = exception.InnerException as PostgresException;

            Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres?.SqlState);
            Assert.Equal("ix_prueba_started_at", postgres?.ConstraintName);
            Assert.NotEmpty(context.ChangeTracker.Entries());
        }
        finally
        {
            await using var cleanup = fixture.CreateContext();
            await cleanup.Database.ExecuteSqlRawAsync(BorrarIndice, CancellationToken.None);
        }
    }

    [Fact]
    public async Task ListarPorEstudiante_FiltraPorEstadoEnLaBase()
    {
        var student = new StudentId(Guid.CreateVersion7());
        var enCurso = new CourseId(Guid.CreateVersion7());
        var finalizado = new CourseId(Guid.CreateVersion7());
        var lesson = new LessonId(Guid.CreateVersion7());

        await using (var writeContext = fixture.CreateContext())
        {
            writeContext.CourseProgresses.Add(CourseProgress.Start(student, enCurso, StartedAt));

            var sellado = CourseProgress.Start(student, finalizado, StartedAt);
            sellado.MarkLessonCompleted(lesson, Publicadas(lesson), FirstMark);
            writeContext.CourseProgresses.Add(sellado);

            await writeContext.SaveChangesAsync(CancellationToken.None);
        }

        await using var readContext = fixture.CreateContext();
        var repository = new CourseProgressRepository(readContext);

        var completados = await repository.ListByStudentAsync(
            student, CourseProgressStatus.Completed, CancellationToken.None);

        var todos = await repository.ListByStudentAsync(student, null, CancellationToken.None);

        Assert.Equal([finalizado], completados.Select(progress => progress.CourseId));
        Assert.Equal(2, todos.Count);
        Assert.All(todos, progress => Assert.Equal(student, progress.StudentId));
    }

    private static UnitOfWork CreateUnitOfWork(LearningDbContext context) => new(context);

    private static Task<CourseProgress?> FindAsync(
        LearningDbContext context,
        StudentId student,
        CourseId course) =>
        context.CourseProgresses
            .Include(nameof(CourseProgress.CompletedLessons))
            .FirstOrDefaultAsync(
                progress => progress.StudentId == student && progress.CourseId == course,
                CancellationToken.None);

    private static IReadOnlySet<LessonId> Publicadas(params LessonId[] lessons) => new HashSet<LessonId>(lessons);
}