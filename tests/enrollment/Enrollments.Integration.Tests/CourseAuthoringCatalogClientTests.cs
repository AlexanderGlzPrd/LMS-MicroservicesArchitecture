using System.Net;

using Enrollments.Application.Abstractions;
using Enrollments.Domain.Enrollments;
using Enrollments.Infrastructure.Acl;

namespace Enrollments.Integration.Tests;

public sealed class CourseAuthoringCatalogClientTests
{
    private static readonly CourseId Course = new(Guid.CreateVersion7());

    [Fact]
    public async Task Check_Con200_EsAvailable()
    {
        var availability = await CheckAsync(StubHttpMessageHandler.Returning(HttpStatusCode.OK));

        Assert.Equal(CourseAvailability.Available, availability);
    }

    [Fact]
    public async Task Check_Con404_EsNotAvailable()
    {
        var availability = await CheckAsync(StubHttpMessageHandler.Returning(HttpStatusCode.NotFound));

        Assert.Equal(CourseAvailability.NotAvailable, availability);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData((HttpStatusCode)418)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.Found)]
    public async Task Check_ConCualquierOtroCodigo_EsUnknown(HttpStatusCode statusCode)
    {
        var availability = await CheckAsync(StubHttpMessageHandler.Returning(statusCode));

        Assert.Equal(CourseAvailability.Unknown, availability);
    }

    [Fact]
    public async Task Check_ConConexionRechazada_EsUnknown()
    {
        var handler = StubHttpMessageHandler.Throwing(
            new HttpRequestException("Connection refused", null, HttpStatusCode.ServiceUnavailable));

        Assert.Equal(CourseAvailability.Unknown, await CheckAsync(handler));
    }

    [Fact]
    public async Task Check_ConRespuestaMasLentaQueElTimeout_EsUnknown()
    {
        var handler = StubHttpMessageHandler.Delaying(TimeSpan.FromSeconds(30));

        var availability = await CheckAsync(handler, timeout: TimeSpan.FromMilliseconds(100));

        Assert.Equal(CourseAvailability.Unknown, availability);
    }

    [Fact]
    public async Task Check_ConCancelacionDelLlamante_PropagaLaCancelacion()
    {
        var handler = StubHttpMessageHandler.Delaying(TimeSpan.FromSeconds(30));
        var client = CreateClient(handler, TimeSpan.FromSeconds(30));

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.CheckAsync(Course, cancellation.Token));
    }

    [Fact]
    public async Task Check_ConsultaElCatalogoPorSuIdentificadorYNadaMas()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.OK);

        await CheckAsync(handler);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal($"/api/v1/catalog/courses/{Course.Value}", request.RequestUri?.AbsolutePath);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, CourseAvailability.Available)]
    [InlineData(HttpStatusCode.NotFound, CourseAvailability.NotAvailable)]
    [InlineData(HttpStatusCode.InternalServerError, CourseAvailability.Unknown)]
    public async Task Check_NoLeeElCuerpoDeLaRespuesta(
        HttpStatusCode statusCode,
        CourseAvailability expected)
    {
        var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(statusCode) { Content = new ExplodingHttpContent() }));

        Assert.Equal(expected, await CheckAsync(handler));
    }

    private static Task<CourseAvailability> CheckAsync(
        StubHttpMessageHandler handler,
        TimeSpan? timeout = null) =>
        CreateClient(handler, timeout ?? TimeSpan.FromSeconds(3)).CheckAsync(Course, CancellationToken.None);

    private static CourseAuthoringCatalogClient CreateClient(
        StubHttpMessageHandler handler,
        TimeSpan timeout) =>
        new(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://course-authoring.test/"),
            Timeout = timeout,
        });
}
