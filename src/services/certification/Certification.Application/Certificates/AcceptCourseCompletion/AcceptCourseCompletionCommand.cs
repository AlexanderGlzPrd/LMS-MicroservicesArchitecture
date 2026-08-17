namespace Certification.Application.Certificates.AcceptCourseCompletion;
public sealed record AcceptCourseCompletionCommand(
    Guid MessageId,
    string MessageType,
    Guid StudentId,
    Guid CourseId,
    DateTimeOffset CompletedAt);