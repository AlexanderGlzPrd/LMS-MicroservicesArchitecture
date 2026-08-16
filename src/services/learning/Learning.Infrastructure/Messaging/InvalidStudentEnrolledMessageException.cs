namespace Learning.Infrastructure.Messaging;
internal sealed class InvalidStudentEnrolledMessageException(string reason)
    : Exception($"El mensaje StudentEnrolled no es valido: {reason}");
