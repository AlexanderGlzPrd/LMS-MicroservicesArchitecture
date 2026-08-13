using Enrollments.Application.Abstractions.Exceptions;
using Enrollments.Domain.Enrollments;
using Enrollments.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Enrollments.Integration.Tests;

public sealed class EnrollmentRepositoryTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly DateTimeOffset EnrolledAt = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Enrollment_SePersisteYSeReleeConTodosSusCampos()
    {
        var id = new EnrollmentId(Guid.CreateVersion7());
        var studentId = new StudentId(Guid.CreateVersion7());
        var courseId = new CourseId(Guid.CreateVersion7());

        await using (var writeContext = fixture.CreateContext())
        {
            writeContext.Enrollments.Add(Enrollment.GrantFree(id, studentId, courseId, EnrolledAt));
            await writeContext.SaveChangesAsync(CancellationToken.None);
        }

        await using var readContext = fixture.CreateContext();
        var persisted = await readContext.Enrollments
            .SingleOrDefaultAsync(enrollment => enrollment.Id == id, CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Equal(studentId, persisted.StudentId);
        Assert.Equal(courseId, persisted.CourseId);
        Assert.Equal(EnrollmentType.Free, persisted.Type);
        Assert.Equal(EnrolledAt, persisted.EnrolledAt);
    }

    [Fact]
    public async Task ParejaRepetida_LaRechazaElIndiceUnico()
    {
        var studentId = new StudentId(Guid.CreateVersion7());
        var courseId = new CourseId(Guid.CreateVersion7());

        await using (var firstContext = fixture.CreateContext())
        {
            firstContext.Enrollments.Add(Enrollment.GrantFree(
                new EnrollmentId(Guid.CreateVersion7()), studentId, courseId, EnrolledAt));
            await firstContext.SaveChangesAsync(CancellationToken.None);
        }

        await using var secondContext = fixture.CreateContext();
        secondContext.Enrollments.Add(Enrollment.GrantFree(
            new EnrollmentId(Guid.CreateVersion7()), studentId, courseId, EnrolledAt));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => secondContext.SaveChangesAsync(CancellationToken.None));

        Assert.Contains("ix_enrollments_student_id_course_id", exception.InnerException?.Message);
    }

    [Fact]
    public async Task ParejaRepetida_ElUnitOfWorkLaTraduceADuplicateEnrollment()
    {
        var studentId = new StudentId(Guid.CreateVersion7());
        var courseId = new CourseId(Guid.CreateVersion7());

        await using (var firstContext = fixture.CreateContext())
        {
            firstContext.Enrollments.Add(Enrollment.GrantFree(
                new EnrollmentId(Guid.CreateVersion7()), studentId, courseId, EnrolledAt));
            await firstContext.SaveChangesAsync(CancellationToken.None);
        }

        await using var secondContext = fixture.CreateContext();
        secondContext.Enrollments.Add(Enrollment.GrantFree(
            new EnrollmentId(Guid.CreateVersion7()), studentId, courseId, EnrolledAt));

        var unitOfWork = new UnitOfWork(secondContext);

        await Assert.ThrowsAsync<DuplicateEnrollmentException>(
            () => unitOfWork.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ListadoPorEstudiante_DevuelveSoloLasSuyasOrdenadasPorFechaDescendente()
    {
        var studentId = new StudentId(Guid.CreateVersion7());
        var otherStudent = new StudentId(Guid.CreateVersion7());
        var older = new CourseId(Guid.CreateVersion7());
        var newer = new CourseId(Guid.CreateVersion7());

        await using (var writeContext = fixture.CreateContext())
        {
            writeContext.Enrollments.AddRange(
                Enrollment.GrantFree(
                    new EnrollmentId(Guid.CreateVersion7()), studentId, older, EnrolledAt),
                Enrollment.GrantFree(
                    new EnrollmentId(Guid.CreateVersion7()), studentId, newer, EnrolledAt.AddHours(1)),
                Enrollment.GrantFree(
                    new EnrollmentId(Guid.CreateVersion7()), otherStudent, older, EnrolledAt));

            await writeContext.SaveChangesAsync(CancellationToken.None);
        }

        await using var readContext = fixture.CreateContext();
        var repository = new EnrollmentRepository(readContext);

        var mine = await repository.ListByStudentAsync(studentId, CancellationToken.None);

        Assert.Equal([newer, older], mine.Select(enrollment => enrollment.CourseId));
    }

    [Fact]
    public async Task Busqueda_DeUnaParejaSinMatricula_DevuelveNull()
    {
        await using var context = fixture.CreateContext();
        var repository = new EnrollmentRepository(context);

        var missing = await repository.FindAsync(
            new StudentId(Guid.CreateVersion7()),
            new CourseId(Guid.CreateVersion7()),
            CancellationToken.None);

        Assert.Null(missing);
    }
}
