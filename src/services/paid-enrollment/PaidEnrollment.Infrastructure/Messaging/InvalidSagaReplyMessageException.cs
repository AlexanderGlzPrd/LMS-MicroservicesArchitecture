namespace PaidEnrollment.Infrastructure.Messaging;
internal sealed class InvalidSagaReplyMessageException(string reply, string reason)
    : Exception($"La respuesta {reply} no es valida: {reason}");