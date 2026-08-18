using Enrollments.Application.Abstractions;
using Enrollments.Application.Enrollments.GrantEnrollmentForCapturedPayment;
using Enrollments.Domain.Enrollments;
using MassTransit;
using Microsoft.Extensions.Logging;
using PaidEnrollment.Contracts.V1;
namespace Enrollments.Infrastructure.Messaging;

internal sealed class GrantEnrollmentForCapturedPaymentConsumer(
    GrantEnrollmentForCapturedPaymentHandler handler,
    ILogger<GrantEnrollmentForCapturedPaymentConsumer> logger)
    : IConsumer<GrantEnrollmentForCapturedPayment>
{
    private static readonly EventId PurchaseIdConflictEvent = new(7001, "purchase-id-conflict");

    public async Task Consume(ConsumeContext<GrantEnrollmentForCapturedPayment> context)
    {
        var messageId = context.MessageId
            ?? throw new InvalidGrantEnrollmentMessageException("el sobre no trae MessageId.");

        var message = context.Message;

        if (message.PurchaseId == Guid.Empty)
        {
            throw new InvalidGrantEnrollmentMessageException("PurchaseId esta a ceros.");
        }

        if (message.StudentId == Guid.Empty)
        {
            throw new InvalidGrantEnrollmentMessageException("StudentId esta a ceros.");
        }

        if (message.CourseId == Guid.Empty)
        {
            throw new InvalidGrantEnrollmentMessageException("CourseId esta a ceros.");
        }

        if (message.OccurredAt == default)
        {
            throw new InvalidGrantEnrollmentMessageException("OccurredAt no tiene valor.");
        }

        var messageType = typeof(GrantEnrollmentForCapturedPayment).FullName!;

        var outcome = await handler.HandleAsync(
            new GrantEnrollmentForCapturedPaymentCommand(
                messageId,
                messageType,
                new PurchaseId(message.PurchaseId),
                new StudentId(message.StudentId),
                new CourseId(message.CourseId),
                message.OccurredAt),
            context.CancellationToken);

        if (outcome is GrantEnrollmentOutcome.PurchaseIdConflict)
        {
            logger.LogError(
                PurchaseIdConflictEvent,
                "La compra {PurchaseId} ya consta en el ledger con otra pareja de estudiante y "
                + "curso. La entrega declara {StudentId}/{CourseId} y se rechaza sin conceder "
                + "nada ni tocar el ledger.",
                message.PurchaseId,
                message.StudentId,
                message.CourseId);
        }
    }
}
