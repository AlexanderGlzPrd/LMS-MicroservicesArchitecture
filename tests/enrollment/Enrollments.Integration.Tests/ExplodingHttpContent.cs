using System.Net;
namespace Enrollments.Integration.Tests;

internal sealed class ExplodingHttpContent : HttpContent
{
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
        throw new InvalidOperationException("El adaptador no debe leer el cuerpo de la respuesta.");

    protected override bool TryComputeLength(out long length)
    {
        length = 0;

        return false;
    }
}
