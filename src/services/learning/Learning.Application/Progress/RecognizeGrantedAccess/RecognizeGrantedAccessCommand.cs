using Learning.Domain.Progress;
namespace Learning.Application.Progress.RecognizeGrantedAccess;
public sealed record RecognizeGrantedAccessCommand(
    Guid MessageId,
    string MessageType,
    StudentId StudentId,
    CourseId CourseId,
    DateTimeOffset OccurredAt);