using System.Text.Json;
using BuildingBlocks.Messaging;
using BuildingBlocks.Observability;
using Learning.Contracts.V1;
using Learning.Infrastructure.Persistence;
namespace Learning.Infrastructure.Messaging;
internal sealed class OutboxWriter(LearningDbContext context)
{
    public void Enqueue(CourseCompleted contract)
    {
        context.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            StudentId = contract.StudentId,
            CourseId = contract.CourseId,
            MessageType = OutboxContractMapper.CourseCompletedType,
            Payload = JsonSerializer.Serialize(contract, OutboxSerialization.Options),
            OccurredAt = contract.CompletedAt,
            TraceContext = OutboxTraceContext.Capture(),
        });
    }
}
