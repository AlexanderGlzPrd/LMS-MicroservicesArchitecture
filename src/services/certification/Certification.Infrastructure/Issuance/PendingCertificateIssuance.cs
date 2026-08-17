namespace Certification.Infrastructure.Issuance;
internal sealed class PendingCertificateIssuance
{
    public required Guid StudentId { get; init; }

    public required Guid CourseId { get; init; }

    public required DateTimeOffset CompletedAt { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public int AttemptCount { get; set; }

    public string? LastError { get; set; }

    public DateTimeOffset? LastAttemptAt { get; set; }
}