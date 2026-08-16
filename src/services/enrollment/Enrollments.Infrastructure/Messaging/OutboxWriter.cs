using System.Text.Json;
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