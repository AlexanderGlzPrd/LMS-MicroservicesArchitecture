namespace PaymentProviderSim.Worker.Persistence;
internal sealed class DuplicatePaymentCommandException(Exception innerException)
    : Exception("El comando ya habia sido aplicado por otra entrega simultanea.", innerException);