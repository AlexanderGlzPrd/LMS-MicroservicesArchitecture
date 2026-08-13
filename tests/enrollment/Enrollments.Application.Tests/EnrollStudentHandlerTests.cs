using Enrollments.Application.Abstractions;
using Enrollments.Application.Abstractions.Exceptions;
using Enrollments.Application.Enrollments.EnrollStudent;
using Enrollments.Application.Tests.Fakes;
using Enrollments.Domain.Enrollments;

namespace Enrollments.Application.Tests;

public sealed class EnrollStudentHandlerTests
{
    private static readonly StudentId Student = new(Guid.CreateVersion7());
    private static readonly CourseId Course = new(Guid.CreateVersion7());
    private static readonly DateTimeOffset Now = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryEnrollmentRepository enrollments = new();
    private readonly StubCurrentActor currentActor = new(Student);
    private readonly FixedTimeProvider timeProvider = new(Now);

    [Fact]
    public async Task Matricular_ConCursoDisponible_CreaLaMatricula()
    {
        var availability = new StubCourseAvailability(CourseAvailability.Available);
        var unitOfWork = new NoOpUnitOfWork(enrollments);

        var result = await HandleAsync(availability, unitOfWork);

        Assert.True(result.Created);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
        Assert.Equal(1, enrollments.StoredCount);
    }

    [Fact]
    public async Task Matricular_ConCursoDisponible_DevuelveLaVistaDeLaMatriculaCreada()
    {
        var availability = new StubCourseAvailability(CourseAvailability.Available);

        var result = await HandleAsync(availability, new NoOpUnitOfWork(enrollments));

        Assert.Equal(Student.Value, result.Enrollment.StudentId);
        Assert.Equal(Course.Value, result.Enrollment.CourseId);
        Assert.Equal(nameof(EnrollmentType.Free), result.Enrollment.Type);
        Assert.Equal(Now, result.Enrollment.EnrolledAt);
    }

    [Fact]
    public async Task Matricular_ConMatriculaPreexistente_NoPersisteNiConsultaACourseAuthoring()
    {
        var existing = Enrollment.GrantFree(
            new EnrollmentId(Guid.CreateVersion7()), Student, Course, Now.AddDays(-1));
        enrollments.Seed(existing);

        var availability = new StubCourseAvailability(CourseAvailability.Available);
        var unitOfWork = new NoOpUnitOfWork(enrollments);

        var result = await HandleAsync(availability, unitOfWork);

        Assert.False(result.Created);
        Assert.Equal(existing.Id.Value, result.Enrollment.Id);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
        Assert.Equal(0, availability.CheckCount);
    }

    [Fact]
    public async Task Matricular_ConCursoNoDisponible_LanzaYNoPersiste()
    {
        var availability = new StubCourseAvailability(CourseAvailability.NotAvailable);
        var unitOfWork = new NoOpUnitOfWork(enrollments);

        await Assert.ThrowsAsync<CourseNotAvailableException>(
            () => HandleAsync(availability, unitOfWork));

        Assert.Equal(0, unitOfWork.SaveChangesCount);
        Assert.Equal(0, enrollments.StoredCount);
    }

    [Fact]
    public async Task Matricular_ConDisponibilidadDesconocida_LanzaYNoPersiste()
    {
        var availability = new StubCourseAvailability(CourseAvailability.Unknown);
        var unitOfWork = new NoOpUnitOfWork(enrollments);

        await Assert.ThrowsAsync<CourseAvailabilityUnknownException>(
            () => HandleAsync(availability, unitOfWork));

        Assert.Equal(0, unitOfWork.SaveChangesCount);
        Assert.Equal(0, enrollments.StoredCount);
    }

    [Fact]
    public async Task Matricular_ConCarreraPerdida_ReleeYDevuelveLaMatriculaGanadora()
    {
        var winner = Enrollment.GrantFree(
            new EnrollmentId(Guid.CreateVersion7()), Student, Course, Now.AddSeconds(-1));

        var availability = new StubCourseAvailability(CourseAvailability.Available);
        var unitOfWork = new NoOpUnitOfWork(enrollments)
        {
            BeforeSave = () => enrollments.Seed(winner),
            ThrowOnSave = new DuplicateEnrollmentException(new InvalidOperationException("23505")),
        };

        var result = await HandleAsync(availability, unitOfWork);

        Assert.False(result.Created);
        Assert.Equal(winner.Id.Value, result.Enrollment.Id);
        Assert.Equal(1, enrollments.StoredCount);
    }

    private Task<EnrollStudentResult> HandleAsync(
        StubCourseAvailability availability,
        NoOpUnitOfWork unitOfWork)
    {
        var handler = new EnrollStudentHandler(
            enrollments,
            unitOfWork,
            availability,
            currentActor,
            timeProvider);

        return handler.HandleAsync(new EnrollStudentCommand(Course), CancellationToken.None);
    }
}
