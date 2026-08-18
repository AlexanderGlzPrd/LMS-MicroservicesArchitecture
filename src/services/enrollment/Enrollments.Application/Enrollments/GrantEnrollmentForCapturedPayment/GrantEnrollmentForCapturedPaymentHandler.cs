using Enrollments.Application.Abstractions;
using Enrollments.Application.Abstractions.Exceptions;
using Enrollments.Domain.Enrollments;
namespace Enrollments.Application.Enrollments.GrantEnrollmentForCapturedPayment;

public sealed class GrantEnrollmentForCapturedPaymentHandler(
    IEnrollmentRepository enrollments,
    IPurchaseGrantLedger purchaseGrants,
    IInbox inbox,
    IOutbox outbox,
    IUnitOfWork unitOfWork,
    ICourseAvailability courseAvailability,
    TimeProvider timeProvider)
{
    public async Task<GrantEnrollmentOutcome> HandleAsync(
        GrantEnrollmentForCapturedPaymentCommand command,
        CancellationToken cancellationToken)
    {
        if (await inbox.HasBeenProcessedAsync(command.MessageId, cancellationToken))
        {
            return GrantEnrollmentOutcome.AlreadyProcessed;
        }

        var ledger = await purchaseGrants.FindAsync(command.PurchaseId, cancellationToken);

        if (ledger is not null)
        {
            return await ReplyFromLedgerAsync(command, ledger, cancellationToken);
        }

        var existing = await enrollments.FindAsync(
            command.StudentId, command.CourseId, cancellationToken);

        if (existing is not null)
        {
            return await RecordAlreadyExistedAsync(command, cancellationToken);
        }

        var availability = await courseAvailability.CheckAsync(
            command.CourseId, cancellationToken);

        if (availability is CourseAvailability.NotAvailable)
        {
            return await RecordRejectionAsync(
                command, GrantRejectionReasons.CourseNotAvailable, cancellationToken);
        }

        if (availability is CourseAvailability.Unknown)
        {
            throw new CourseAvailabilityUnknownException(command.CourseId);
        }

        return await GrantAsync(command, cancellationToken);
    }

    private async Task<GrantEnrollmentOutcome> GrantAsync(
        GrantEnrollmentForCapturedPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        var enrollment = Enrollment.GrantPaid(
            new EnrollmentId(Guid.CreateVersion7()),
            command.StudentId,
            command.CourseId,
            now);

        enrollments.Add(enrollment);
        outbox.EnqueueStudentEnrolled(enrollment);

        var entry = BuildEntry(
            command,
            PurchaseGrantOutcome.Created,
            PurchaseGrantOrigin.ThisPurchase,
            rejectionReason: null,
            now);

        purchaseGrants.Add(entry);
        outbox.EnqueueEnrollmentGranted(command.MessageId, entry);
        inbox.Record(command.MessageId, command.MessageType, now);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateEnrollmentException)
        {
            return await RecordAlreadyExistedAsync(command, cancellationToken);
        }
        catch (DuplicatePurchaseGrantException)
        {
            var winner = await RequireLedgerAsync(command, cancellationToken);

            return await ReplyFromLedgerAsync(command, winner, cancellationToken);
        }

        return GrantEnrollmentOutcome.Created;
    }

    private async Task<GrantEnrollmentOutcome> RecordAlreadyExistedAsync(
        GrantEnrollmentForCapturedPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var entry = BuildEntry(
            command,
            PurchaseGrantOutcome.AlreadyExisted,
            PurchaseGrantOrigin.Other,
            rejectionReason: null,
            timeProvider.GetUtcNow());

        purchaseGrants.Add(entry);
        outbox.EnqueueEnrollmentGranted(command.MessageId, entry);
        inbox.Record(command.MessageId, command.MessageType, entry.ProcessedAt);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicatePurchaseGrantException)
        {
            var winner = await RequireLedgerAsync(command, cancellationToken);
            return await ReplyFromLedgerAsync(command, winner, cancellationToken);
        }

        return GrantEnrollmentOutcome.AlreadyExisted;
    }

    private async Task<GrantEnrollmentOutcome> RecordRejectionAsync(
        GrantEnrollmentForCapturedPaymentCommand command,
        string reason,
        CancellationToken cancellationToken)
    {
        var entry = BuildEntry(
            command,
            PurchaseGrantOutcome.Rejected,
            PurchaseGrantOrigin.None,
            reason,
            timeProvider.GetUtcNow());

        purchaseGrants.Add(entry);
        outbox.EnqueueEnrollmentRejected(command.MessageId, entry);
        inbox.Record(command.MessageId, command.MessageType, entry.ProcessedAt);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicatePurchaseGrantException)
        {
            var winner = await RequireLedgerAsync(command, cancellationToken);

            return await ReplyFromLedgerAsync(command, winner, cancellationToken);
        }

        return GrantEnrollmentOutcome.Rejected;
    }

    private async Task<GrantEnrollmentOutcome> ReplyFromLedgerAsync(
        GrantEnrollmentForCapturedPaymentCommand command,
        PurchaseGrantEntry ledger,
        CancellationToken cancellationToken)
    {
        if (ledger.StudentId != command.StudentId || ledger.CourseId != command.CourseId)
        {
            return await RejectAsConflictAsync(command, cancellationToken);
        }

        if (ledger.Outcome is PurchaseGrantOutcome.Rejected)
        {
            outbox.EnqueueEnrollmentRejected(command.MessageId, ledger);
        }
        else
        {
            outbox.EnqueueEnrollmentGranted(command.MessageId, ledger);
        }

        inbox.Record(command.MessageId, command.MessageType, timeProvider.GetUtcNow());

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ledger.Outcome is PurchaseGrantOutcome.Rejected
            ? GrantEnrollmentOutcome.Rejected
            : GrantEnrollmentOutcome.AlreadyExisted;
    }

    private async Task<GrantEnrollmentOutcome> RejectAsConflictAsync(
        GrantEnrollmentForCapturedPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var conflict = BuildEntry(
            command,
            PurchaseGrantOutcome.Rejected,
            PurchaseGrantOrigin.None,
            GrantRejectionReasons.PurchaseIdConflict,
            timeProvider.GetUtcNow());

        outbox.EnqueueEnrollmentRejected(command.MessageId, conflict);
        inbox.Record(command.MessageId, command.MessageType, conflict.ProcessedAt);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return GrantEnrollmentOutcome.PurchaseIdConflict;
    }

    private async Task<PurchaseGrantEntry> RequireLedgerAsync(
        GrantEnrollmentForCapturedPaymentCommand command,
        CancellationToken cancellationToken)
        => await purchaseGrants.FindAsync(command.PurchaseId, cancellationToken)
           ?? throw new InvalidOperationException(
               $"El ledger rechazo la concesion de la compra '{command.PurchaseId.Value}', "
               + "pero la entrada existente no se ha podido releer.");

    private static PurchaseGrantEntry BuildEntry(
        GrantEnrollmentForCapturedPaymentCommand command,
        PurchaseGrantOutcome outcome,
        PurchaseGrantOrigin origin,
        string? rejectionReason,
        DateTimeOffset processedAt) => new()
        {
            PurchaseId = command.PurchaseId,
            StudentId = command.StudentId,
            CourseId = command.CourseId,
            Outcome = outcome,
            Origin = origin,
            RejectionReason = rejectionReason,
            InitialMessageId = command.MessageId,
            ProcessedAt = processedAt,
        };
}
