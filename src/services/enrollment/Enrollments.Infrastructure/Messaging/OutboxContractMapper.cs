using Enrollments.Contracts.V1;
namespace Enrollments.Infrastructure.Messaging;
internal static class OutboxContractMapper
{
    public static readonly string StudentEnrolledType = typeof(StudentEnrolled).FullName!;
}