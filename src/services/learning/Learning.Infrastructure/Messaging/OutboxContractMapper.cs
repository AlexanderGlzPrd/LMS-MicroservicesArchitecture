using Learning.Contracts.V1;
namespace Learning.Infrastructure.Messaging;
internal static class OutboxContractMapper
{
    public static readonly string CourseCompletedType = typeof(CourseCompleted).FullName!;
}
