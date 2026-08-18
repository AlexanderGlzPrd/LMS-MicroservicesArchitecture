using System.Text.Json;
using BuildingBlocks.Messaging;
using Enrollments.Application.Abstractions;
using Enrollments.Contracts.V1;
using Enrollments.Domain.Enrollments;
using Enrollments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace Enrollments.Infrastructure.Messaging;
internal sealed class OutboxWriter(EnrollmentsDbContext context) : IOutbox
{
    public void EnqueueStudentEnrolled(Enrollment enrollment)
    {
        context.OutboxMessages.Add(Build(enrollment));
    }

    public async Task<bool> EnsureStudentEnrolledAsync(
        Enrollment enrollment,
        CancellationToken cancellationToken)
    {
        var aggregateId = enrollment.Id.Value;

        var alreadyEnqueued = await context.OutboxMessages.AnyAsync(
            message => message.AggregateId == aggregateId
                && message.MessageType == OutboxContractMapper.StudentEnrolledType,
            cancellationToken);

        if (alreadyEnqueued)
        {
            return false;
        }

        context.OutboxMessages.Add(Build(enrollment));

        return true;
    }

    public void EnqueueEnrollmentGranted(Guid incomingMessageId, PurchaseGrantEntry entry)
    {
        var contract = new EnrollmentGranted
        {
            PurchaseId = entry.PurchaseId.Value,
            StudentId = entry.StudentId.Value,
            CourseId = entry.CourseId.Value,
            Outcome = entry.Outcome.ToString(),
            Origin = entry.Origin.ToString(),
            OccurredAt = entry.ProcessedAt,
        };

        context.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            AggregateId = incomingMessageId,
            MessageType = OutboxContractMapper.EnrollmentGrantedType,
            RoutingKey = OutboxContractMapper.EnrollmentGrantedRoutingKey,
            Payload = JsonSerializer.Serialize(contract, OutboxSerialization.Options),
            OccurredAt = entry.ProcessedAt,
        });
    }

    public void EnqueueEnrollmentRejected(Guid incomingMessageId, PurchaseGrantEntry entry)
    {
        var contract = new EnrollmentRejected
        {
            PurchaseId = entry.PurchaseId.Value,
            StudentId = entry.StudentId.Value,
            CourseId = entry.CourseId.Value,
            Reason = entry.RejectionReason
                ?? throw new InvalidOperationException(
                    "Un rechazo de concesion no puede viajar sin razon."),
            OccurredAt = entry.ProcessedAt,
        };

        context.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            AggregateId = incomingMessageId,
            MessageType = OutboxContractMapper.EnrollmentRejectedType,
            RoutingKey = OutboxContractMapper.EnrollmentRejectedRoutingKey,
            Payload = JsonSerializer.Serialize(contract, OutboxSerialization.Options),
            OccurredAt = entry.ProcessedAt,
        });
    }

    private static OutboxMessage Build(Enrollment enrollment)
    {
        var contract = new StudentEnrolled
        {
            StudentId = enrollment.StudentId.Value,
            CourseId = enrollment.CourseId.Value,
            OccurredAt = enrollment.EnrolledAt,
        };

        return new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            AggregateId = enrollment.Id.Value,
            MessageType = OutboxContractMapper.StudentEnrolledType,
            Payload = JsonSerializer.Serialize(contract, OutboxSerialization.Options),
            OccurredAt = enrollment.EnrolledAt,
        };
    }
}
