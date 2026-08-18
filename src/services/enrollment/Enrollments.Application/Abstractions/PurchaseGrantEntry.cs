using Enrollments.Domain.Enrollments;
namespace Enrollments.Application.Abstractions;
public sealed record PurchaseGrantEntry
{
    public required PurchaseId PurchaseId { get; init; }
    public required StudentId StudentId { get; init; }
    public required CourseId CourseId { get; init; }
    public required PurchaseGrantOutcome Outcome { get; init; }
    public required PurchaseGrantOrigin Origin { get; init; }
    public required string? RejectionReason { get; init; }
    public required Guid InitialMessageId { get; init; }
    public required DateTimeOffset ProcessedAt { get; init; }
}
