using System.Net;
using Learning.Application.Abstractions;
using Learning.Domain.Progress;
using Learning.Infrastructure.Acl;
namespace Learning.Integration.Tests;

public sealed class CourseAuthoringLessonSetClientTests
{
    private static readonly CourseId Course = new(Guid.CreateVersion7());
    private static readonly Guid FirstLesson = Guid.CreateVersion7();
    private static readonly Guid SecondLesson = Guid.CreateVersion7();

    [Fact]
    public async Task Con200Valido_EsAvailableConElConjuntoExacto()
    {
        var handler = StubHttpMessageHandler.ReturningJson(
            $$"""{"courseId":"{{Course.Value}}","lessonIds":["{{FirstLesson}}","{{SecondLesson}}"]}""");

        var set = await GetAsync(handler);

        Assert.Equal(CurrentLessonSetStatus.Available, set.Status);
        Assert.Equal(
            new HashSet<LessonId> { new(FirstLesson), new(SecondLesson) },
            set.LessonIds);
    }

    [Fact]
    public async Task Con404_EsNotAvailable()
    {
        var set = await GetAsync(StubHttpMessageHandler.Returning(HttpStatusCode.NotFound));

        Assert.Equal(CurrentLessonSetStatus.NotAvailable, set.Status);
        Assert.Empty(set.LessonIds);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData((HttpStatusCode)418)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.Found)]
    public async Task ConCualquierOtroCodigo_EsUnknown(HttpStatusCode statusCode)
    {
        var set = await GetAsync(StubHttpMessageHandler.Returning(statusCode));

        Assert.Equal(CurrentLessonSetStatus.Unknown, set.Status);
        Assert.Empty(set.LessonIds);
    }

    [Fact]
    public async Task ConConexionRechazada_EsUnknown()
    {
        var handler = StubHttpMessageHandler.Throwing(
            new HttpRequestException("Connection refused", null, HttpStatusCode.ServiceUnavailable));

        Assert.Equal(CurrentLessonSetStatus.Unknown, (await GetAsync(handler)).Status);
    }

    [Fact]
    public async Task ConRespuestaMasLentaQueElTimeout_EsUnknown()
    {
        var handler = StubHttpMessageHandler.Delaying(TimeSpan.FromSeconds(30));

        var set = await GetAsync(handler, timeout: TimeSpan.FromMilliseconds(100));

        Assert.Equal(CurrentLessonSetStatus.Unknown, set.Status);
    }

    [Fact]
    public async Task ConCancelacionDelLlamante_PropagaLaCancelacion()
    {
        var handler = StubHttpMessageHandler.Delaying(TimeSpan.FromSeconds(30));
        var client = CreateClient(handler, TimeSpan.FromSeconds(30));

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetAsync(Course, cancellation.Token));
    }

    [Fact]
    public async Task ConsultaSoloElEndpointDeIdentificadoresDeLeccion()
    {
        var handler = StubHttpMessageHandler.ReturningJson(
            $$"""{"courseId":"{{Course.Value}}","lessonIds":["{{FirstLesson}}"]}""");

        await GetAsync(handler);

        var request = Assert.Single(handler.Requests);

        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(
            $"/api/v1/catalog/courses/{Course.Value}/lesson-ids",
            request.RequestUri?.AbsolutePath);
    }

    public static TheoryData<string, string> CuerposQueIncumplenElContrato() => new()
    {
        { "JSON malformado", """{"courseId": """ },
        { "cuerpo vacio", "" },
        { "courseId ausente", $$"""{"lessonIds":["{{FirstLesson}}"]}""" },
        { "courseId nulo", $$"""{"courseId":null,"lessonIds":["{{FirstLesson}}"]}""" },
        { "courseId a ceros", $$"""{"courseId":"{{Guid.Empty}}","lessonIds":["{{FirstLesson}}"]}""" },
        { "courseId de otro curso", $$"""{"courseId":"{{Guid.CreateVersion7()}}","lessonIds":["{{FirstLesson}}"]}""" },
        { "lessonIds ausente", $$"""{"courseId":"{{Course.Value}}"}""" },
        { "lessonIds nulo", $$"""{"courseId":"{{Course.Value}}","lessonIds":null}""" },
        { "lessonIds vacio", $$"""{"courseId":"{{Course.Value}}","lessonIds":[]}""" },
        { "lessonId a ceros", $$"""{"courseId":"{{Course.Value}}","lessonIds":["{{Guid.Empty}}"]}""" },
        {
            "lessonId duplicada",
            $$"""{"courseId":"{{Course.Value}}","lessonIds":["{{FirstLesson}}","{{FirstLesson}}"]}"""
        },
        {
            "elemento que no es un GUID",
            $$"""{"courseId":"{{Course.Value}}","lessonIds":["no-soy-un-guid"]}"""
        },
        { "elemento nulo dentro del array", $$"""{"courseId":"{{Course.Value}}","lessonIds":[null]}""" },
    };

    [Theory]
    [MemberData(nameof(CuerposQueIncumplenElContrato))]
    public async Task Con200QueIncumpleElContrato_EsUnknown(string caso, string json)
    {
        var set = await GetAsync(StubHttpMessageHandler.ReturningJson(json));

        Assert.Equal(CurrentLessonSetStatus.Unknown, set.Status);
        Assert.Empty(set.LessonIds);
        Assert.NotEmpty(caso);
    }

    private static Task<CurrentLessonSet> GetAsync(
        StubHttpMessageHandler handler,
        TimeSpan? timeout = null) =>
        CreateClient(handler, timeout ?? TimeSpan.FromSeconds(3)).GetAsync(Course, CancellationToken.None);

    private static CourseAuthoringLessonSetClient CreateClient(
        StubHttpMessageHandler handler,
        TimeSpan timeout) =>
        new(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://course-authoring.test/"),
            Timeout = timeout,
        });
}
