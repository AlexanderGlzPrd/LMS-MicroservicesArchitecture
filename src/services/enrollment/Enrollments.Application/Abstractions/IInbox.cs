namespace Enrollments.Application.Abstractions;
public interface IInbox
{
    Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken cancellationToken);
    void Record(Guid messageId, string messageType, DateTimeOffset processedAt);
}