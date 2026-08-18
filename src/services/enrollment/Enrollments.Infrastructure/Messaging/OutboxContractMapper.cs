using Enrollments.Contracts.V1;
namespace Enrollments.Infrastructure.Messaging;
internal static class OutboxContractMapper
{
    public static readonly string StudentEnrolledType = typeof(StudentEnrolled).FullName!;

    public static readonly string EnrollmentGrantedType = typeof(EnrollmentGranted).FullName!;

    public static readonly string EnrollmentRejectedType = typeof(EnrollmentRejected).FullName!;

    public const string EnrollmentGrantedRoutingKey = "enrollment-granted";

    public const string EnrollmentRejectedRoutingKey = "enrollment-rejected";
}
