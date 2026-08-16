using Enrollments.Application.Abstractions;
using Enrollments.Application.Abstractions.Exceptions;
using Enrollments.Domain.Enrollments;
namespace Enrollments.Application.Enrollments.EnrollStudent;

public sealed class EnrollStudentHandler(
    IEnrollmentRepository enrollments,
    IUnitOfWork unitOfWork,
    IOutbox outbox,
    ICourseAvailability courseAvailability,
    ICurrentActor currentActor,
    TimeProvider timeProvider)
{
    public async Task<EnrollStudentResult> HandleAsync(
        EnrollStudentCommand command,
        CancellationToken cancellationToken)
    {
        var studentId = currentActor.StudentId; //id temporal obtenido de la cabecera, se reemplaza con el idtoken del jwt en el futuro

        var existing = await enrollments.FindAsync(studentId, command.CourseId, cancellationToken);

        if (existing is not null)
        {
            return await AlreadyExistedAsync(existing, cancellationToken);
        }

        var availability = await courseAvailability.CheckAsync(command.CourseId, cancellationToken);

        if (availability is CourseAvailability.NotAvailable)
        {
            throw new CourseNotAvailableException(command.CourseId);
        }

        if (availability is CourseAvailability.Unknown)
        {
            throw new CourseAvailabilityUnknownException(command.CourseId);
        }

        var enrollment = Enrollment.GrantFree(
            new EnrollmentId(Guid.CreateVersion7()),
            studentId,
            command.CourseId,
            timeProvider.GetUtcNow());

        enrollments.Add(enrollment);
        outbox.EnqueueStudentEnrolled(enrollment);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateEnrollmentException)
        {
            var winner = await RequireExistingAsync(studentId, command.CourseId, cancellationToken);

            return await AlreadyExistedAsync(winner, cancellationToken);
        }

        return new EnrollStudentResult(EnrollmentView.From(enrollment), Created: true);
    }

    private async Task<EnrollStudentResult> AlreadyExistedAsync(
        Enrollment enrollment,
        CancellationToken cancellationToken)
    {
        var enqueued = await outbox.EnsureStudentEnrolledAsync(enrollment, cancellationToken);

        if (enqueued)
        {
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DuplicateOutboxMessageException)
            {
            }
        }

        return new EnrollStudentResult(EnrollmentView.From(enrollment), Created: false);
    }

    private async Task<Enrollment> RequireExistingAsync(
        StudentId studentId,
        CourseId courseId,
        CancellationToken cancellationToken)
        => await enrollments.FindAsync(studentId, courseId, cancellationToken)
           ?? throw new InvalidOperationException(
               $"El indice unico rechazo la matricula de '{studentId.Value}' en '{courseId.Value}', "
               + "pero la matricula existente no se ha podido releer.");
}
