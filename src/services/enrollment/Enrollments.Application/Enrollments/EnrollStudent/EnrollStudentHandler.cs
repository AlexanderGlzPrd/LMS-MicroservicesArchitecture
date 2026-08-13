using Enrollments.Application.Abstractions;
using Enrollments.Application.Abstractions.Exceptions;
using Enrollments.Domain.Enrollments;
namespace Enrollments.Application.Enrollments.EnrollStudent;

public sealed class EnrollStudentHandler(
    IEnrollmentRepository enrollments,
    IUnitOfWork unitOfWork,
    ICourseAvailability courseAvailability,
    ICurrentActor currentActor,
    TimeProvider timeProvider)
{
    public async Task<EnrollStudentResult> HandleAsync(
        EnrollStudentCommand command,
        CancellationToken cancellationToken)
    {
        var studentId = currentActor.StudentId;

        var existing = await enrollments.FindAsync(studentId, command.CourseId, cancellationToken);

        if (existing is not null)
        {
            return new EnrollStudentResult(EnrollmentView.From(existing), Created: false);
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

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateEnrollmentException)
        {
            return new EnrollStudentResult(
                EnrollmentView.From(await RequireExistingAsync(studentId, command.CourseId, cancellationToken)),
                Created: false);
        }

        return new EnrollStudentResult(EnrollmentView.From(enrollment), Created: true);
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
