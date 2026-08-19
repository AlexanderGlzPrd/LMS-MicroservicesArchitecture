using System.Diagnostics;
using Enrollments.Contracts.V1;
using MassTransit;
using Microsoft.Extensions.Logging;
using PaidEnrollment.Application.Purchases.Workflow;
using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Infrastructure.Messaging;
internal sealed class EnrollmentReplyConsumer(
    PurchaseWorkflow workflow,
    ILogger<EnrollmentReplyConsumer> logger) :
    IConsumer<EnrollmentGranted>,
    IConsumer<EnrollmentRejected>
{
    private static readonly EventId CorrelationMismatchEvent =
        new(8001, "saga-correlation-mismatch");

    private static readonly EventId LateMessageEvent = new(8002, "saga-late-message");

    public async Task Consume(ConsumeContext<EnrollmentGranted> context)
    {
        var message = context.Message;
        var reply = Envelope<EnrollmentGranted>(
            context, message.PurchaseId, message.StudentId, message.CourseId, message.OccurredAt);

        var outcome = Translate(message.Outcome, message.Origin);

        Report(
            reply,
            await workflow.OnEnrollmentGrantedAsync(reply, outcome, context.CancellationToken));
    }

    public async Task Consume(ConsumeContext<EnrollmentRejected> context)
    {
        var message = context.Message;
        var reply = Envelope<EnrollmentRejected>(
            context, message.PurchaseId, message.StudentId, message.CourseId, message.OccurredAt);

        if (string.IsNullOrWhiteSpace(message.Reason))
        {
            throw new InvalidSagaReplyMessageException(
                nameof(EnrollmentRejected), "Reason no tiene valor.");
        }

        Report(reply, await workflow.OnEnrollmentRejectedAsync(reply, context.CancellationToken));
    }

    private void Report(EnrollmentReply reply, ReplyReaction reaction)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["PurchaseId"] = reply.PurchaseId.Value,
            ["MessageId"] = reply.MessageId,
            ["TraceId"] = Activity.Current?.TraceId.ToString(),
        });

        switch (reaction)
        {
            case ReplyReaction.CorrelationMismatch:
                logger.LogError(
                    CorrelationMismatchEvent,
                    "La respuesta {MessageType} declara la compra {PurchaseId} con el estudiante "
                    + "{StudentId} y el curso {CourseId}, que no se corresponden. No se aplica "
                    + "ni se deduplica nada.",
                    reply.MessageType,
                    reply.PurchaseId.Value,
                    reply.StudentId.Value,
                    reply.CourseId.Value);

                throw new SagaCorrelationMismatchException(
                    reply.MessageType, reply.PurchaseId.Value);

            case ReplyReaction.Late:
            case ReplyReaction.NotApplicable:
            case ReplyReaction.EvidenceOnly:
                logger.LogInformation(
                    LateMessageEvent,
                    "La respuesta {MessageType} de la compra {PurchaseId} se registro sin "
                    + "avanzar la Saga: {Reaction}.",
                    reply.MessageType,
                    reply.PurchaseId.Value,
                    reaction);

                break;

            default:
                break;
        }
    }

    private static GrantOutcome Translate(string outcome, string origin) =>
        (outcome, origin) switch
        {
            ("Created", _) => GrantOutcome.Created,
            ("AlreadyExisted", "ThisPurchase") => GrantOutcome.AlreadyExistedThisPurchase,
            ("AlreadyExisted", "Other") => GrantOutcome.AlreadyExistedOther,

            _ => throw new InvalidSagaReplyMessageException(
                nameof(EnrollmentGranted),
                $"la pareja Outcome '{outcome}' y Origin '{origin}' no es reconocible."),
        };

    private static EnrollmentReply Envelope<TContract>(
        ConsumeContext context,
        Guid purchaseId,
        Guid studentId,
        Guid courseId,
        DateTimeOffset occurredAt)
    {
        var name = typeof(TContract).Name;

        var messageId = context.MessageId
            ?? throw new InvalidSagaReplyMessageException(name, "el sobre no trae MessageId.");

        if (purchaseId == Guid.Empty)
        {
            throw new InvalidSagaReplyMessageException(name, "PurchaseId esta a ceros.");
        }

        if (studentId == Guid.Empty)
        {
            throw new InvalidSagaReplyMessageException(name, "StudentId esta a ceros.");
        }

        if (courseId == Guid.Empty)
        {
            throw new InvalidSagaReplyMessageException(name, "CourseId esta a ceros.");
        }

        if (occurredAt == default)
        {
            throw new InvalidSagaReplyMessageException(name, "OccurredAt no tiene valor.");
        }

        Activity.Current?.SetTag("lms.purchase.id", purchaseId);

        return new EnrollmentReply(
            messageId,
            typeof(TContract).FullName!,
            new PurchaseId(purchaseId),
            new StudentId(studentId),
            new CourseId(courseId));
    }
}
