namespace PaymentProviderSim.Worker.Messaging;
internal sealed class InvalidPaymentCommandMessageException(string command, string reason)
    : Exception($"El mensaje {command} no es valido: {reason}");