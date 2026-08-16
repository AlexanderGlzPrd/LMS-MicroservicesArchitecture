using Enrollments.Contracts.V1;
using Learning.Application.Progress.RecognizeGrantedAccess;
using Learning.Domain.Progress;
using MassTransit;
namespace Learning.Infrastructure.Messaging;
internal sealed class StudentEnrolledConsumer(RecognizeGrantedAccessHandler handler)
    : IConsumer<StudentEnrolled>
{
    public async Task Consume(ConsumeContext<StudentEnrolled> context)
    {
        var messageId = context.MessageId
            ?? throw new InvalidStudentEnrolledMessageException("el sobre no trae MessageId.");

        var message = context.Message;

        if (message.StudentId == Guid.Empty)
        {
            throw new InvalidStudentEnrolledMessageException("StudentId esta a ceros.");
        }

        if (message.CourseId == Guid.Empty)
        {
            throw new InvalidStudentEnrolledMessageException("CourseId esta a ceros.");
        }

        if (message.OccurredAt == default)
        {
            throw new InvalidStudentEnrolledMessageException("OccurredAt no tiene valor.");
        }

        var messageType = typeof(StudentEnrolled).FullName!;

        await handler.HandleAsync(
            new RecognizeGrantedAccessCommand(
                messageId,
                messageType,
                new StudentId(message.StudentId),
                new CourseId(message.CourseId),
                message.OccurredAt),
            context.CancellationToken);
    }
}