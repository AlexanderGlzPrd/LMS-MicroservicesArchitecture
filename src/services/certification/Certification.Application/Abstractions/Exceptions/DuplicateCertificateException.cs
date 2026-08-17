namespace Certification.Application.Abstractions.Exceptions;
public sealed class DuplicateCertificateException(Exception innerException)
    : Exception("Esa Finalizacion ya tiene certificado emitido.", innerException);