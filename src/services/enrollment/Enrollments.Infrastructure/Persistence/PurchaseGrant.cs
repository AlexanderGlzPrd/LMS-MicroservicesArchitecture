namespace Enrollments.Infrastructure.Persistence;
internal sealed class PurchaseGrant
{
    public required Guid PurchaseId { get; init; }

    public required Guid StudentId { get; init; }

    public required Guid CourseId { get; init; }

    public required string Outcome { get; init; }

    public required string Origin { get; init; }

    public required string? RejectionReason { get; init; }

    public required Guid InitialMessageId { get; init; }

    public required DateTimeOffset ProcessedAt { get; init; }
}