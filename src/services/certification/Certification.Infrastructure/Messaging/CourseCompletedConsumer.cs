using Certification.Application.Certificates.AcceptCourseCompletion;
using Learning.Contracts.V1;
using MassTransit;
namespace Certification.Infrastructure.Messaging;
internal sealed class CourseCompletedConsumer(AcceptCourseCompletionHandler handler)
    : IConsumer<CourseCompleted>
{
    public async Task Consume(ConsumeContext<CourseCompleted> context)
    {
        var messageId = context.MessageId
            ?? throw new InvalidCourseCompletedMessageException("el sobre no trae MessageId.");

        var message = context.Message;

        if (message.StudentId == Guid.Empty)
        {
            throw new InvalidCourseCompletedMessageException("StudentId esta a ceros.");
        }

        if (message.CourseId == Guid.Empty)
        {
            throw new InvalidCourseCompletedMessageException("CourseId esta a ceros.");
        }

        if (message.CompletedAt == default)
        {
            throw new InvalidCourseCompletedMessageException("CompletedAt no tiene valor.");
        }

        var messageType = typeof(CourseCompleted).FullName!;

        await handler.HandleAsync(
            new AcceptCourseCompletionCommand(
                messageId,
                messageType,
                message.StudentId,
                message.CourseId,
                message.CompletedAt),
            context.CancellationToken);
    }
}
