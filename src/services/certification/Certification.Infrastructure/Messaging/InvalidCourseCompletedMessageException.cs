namespace Certification.Infrastructure.Messaging;
internal sealed class InvalidCourseCompletedMessageException(string reason)
    : Exception($"El mensaje CourseCompleted no es valido: {reason}");
