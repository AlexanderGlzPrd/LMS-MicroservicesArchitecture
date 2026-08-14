using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Learning.Domain.Progress;
namespace Learning.Integration.Tests;

public sealed class CourseProgressApiTests(LearningApiFactory factory) : IClassFixture<LearningApiFactory>
{
    private static readonly Guid Student = Guid.CreateVersion7();

    [Fact]
    public async Task PrimerMarcado_DevuelveEnCursoYCreaUnaFilaEnCadaTabla()
    {
        var (course, first, _) = await PublicarCursoDeDosLecciones();

        var response = await MarcarAsync(course, first);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(Student, body.GetProperty("studentId").GetGuid());
        Assert.Equal(course, body.GetProperty("courseId").GetGuid());
        Assert.Equal(nameof(CourseProgressStatus.InProgress), body.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("completedAt").ValueKind);
        Assert.Equal([first], LessonIds(body));

        Assert.Equal(1, await factory.CountProgressAsync(Student, course));
        Assert.Equal(1, await factory.CountCompletedLessonsAsync(Student, course));
    }

    [Fact]
    public async Task MarcadoRepetido_DevuelveElMismoCuerpoYNoAnadeFilas()
    {
        var (course, first, _) = await PublicarCursoDeDosLecciones();

        var primera = await (await MarcarAsync(course, first)).Content.ReadAsStringAsync();
        var repetida = await (await MarcarAsync(course, first)).Content.ReadAsStringAsync();

        Assert.Equal(primera, repetida);
        Assert.Equal(1, await factory.CountCompletedLessonsAsync(Student, course));
    }

    [Fact]
    public async Task MarcarTodasLasLecciones_SellaEnLaMismaPeticion()
    {
        var (course, first, second) = await PublicarCursoDeDosLecciones();

        await MarcarAsync(course, first);
        var response = await MarcarAsync(course, second);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(nameof(CourseProgressStatus.Completed), body.GetProperty("status").GetString());
        Assert.NotEqual(JsonValueKind.Null, body.GetProperty("completedAt").ValueKind);
    }

    [Fact]
    public async Task MarcarDeNuevoTrasElSellado_NoCambiaLaFechaDeFinalizacion()
    {
        var (course, first, second) = await PublicarCursoDeDosLecciones();

        await MarcarAsync(course, first);
        var sellado = await (await MarcarAsync(course, second))
            .Content.ReadFromJsonAsync<JsonElement>();

        var despues = await (await MarcarAsync(course, first))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            sellado.GetProperty("completedAt").GetDateTimeOffset(),
            despues.GetProperty("completedAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task LeccionFueraDelContenidoPublicado_DevuelveUnprocessableSinCrearNingunaFila()
    {
        var (course, _, _) = await PublicarCursoDeDosLecciones();

        var response = await MarcarAsync(course, Guid.CreateVersion7());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, await factory.CountProgressAsync(Student, course));
    }

    [Fact]
    public async Task CursoNoDisponible_DevuelveUnprocessableSinCrearNingunaFila()
    {
        await factory.ResetAsync();
        factory.LessonSet.NotAvailable();

        var course = Guid.CreateVersion7();
        var response = await MarcarAsync(course, Guid.CreateVersion7());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await factory.CountProgressAsync(Student, course));
    }

    [Fact]
    public async Task ConjuntoNoVerificable_DevuelveServiceUnavailableConRetryAfterYSinFilas()
    {
        await factory.ResetAsync();
        factory.LessonSet.Unknown();

        var course = Guid.CreateVersion7();
        var response = await MarcarAsync(course, Guid.CreateVersion7());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(
            LearningApiFactory.RetryAfterSeconds.ToString(),
            response.Headers.RetryAfter?.Delta?.TotalSeconds.ToString());
        Assert.Equal(0, await factory.CountProgressAsync(Student, course));
    }

    [Fact]
    public async Task Completion_ConTodasLasLeccionesCompletadas_DevuelveCompleted()
    {
        var (course, first, second) = await PublicarCursoDeDosLecciones();

        await MarcarAsync(course, first);
        await MarcarAsync(course, second);

        var response = await ConfirmarAsync(course);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(nameof(CourseProgressStatus.Completed), body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Completion_SinCumplirElCriterio_DevuelveUnprocessableYDejaElProgresoEnCurso()
    {
        var (course, first, _) = await PublicarCursoDeDosLecciones();

        await MarcarAsync(course, first);

        var response = await ConfirmarAsync(course);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var detalle = await (await DetalleAsync(course)).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(nameof(CourseProgressStatus.InProgress), detalle.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Completion_RepetidoTrasElSellado_DevuelveElMismoCompletedAt()
    {
        var (course, first, second) = await PublicarCursoDeDosLecciones();

        await MarcarAsync(course, first);
        await MarcarAsync(course, second);

        var primera = await (await ConfirmarAsync(course)).Content.ReadFromJsonAsync<JsonElement>();
        var repetida = await (await ConfirmarAsync(course)).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            primera.GetProperty("completedAt").GetDateTimeOffset(),
            repetida.GetProperty("completedAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task Completion_SinProgresoDelActor_DevuelveNotFound()
    {
        var (course, _, _) = await PublicarCursoDeDosLecciones();

        var response = await ConfirmarAsync(course);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Completion_ConElConjuntoNoVerificable_DevuelveServiceUnavailableAunqueNoHayaProgreso()
    {
        await factory.ResetAsync();
        factory.LessonSet.Unknown();

        var response = await ConfirmarAsync(Guid.CreateVersion7());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Theory]
    [InlineData("completed-lessons")]
    [InlineData("completion")]
    public async Task EscriturasSinCabecera_DevuelvenBadRequest(string segmento)
    {
        await factory.ResetAsync();

        var response = await factory.CreateAnonymousClient().PostAsJsonAsync(
            $"/api/v1/me/course-progress/{Guid.CreateVersion7()}/{segmento}",
            new { lessonId = Guid.CreateVersion7() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("completed-lessons")]
    [InlineData("completion")]
    public async Task EscriturasConCabeceraACeros_DevuelvenBadRequest(string segmento)
    {
        await factory.ResetAsync();

        var client = factory.CreateClientFor(Guid.Empty);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/me/course-progress/{Guid.CreateVersion7()}/{segmento}",
            new { lessonId = Guid.CreateVersion7() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CourseIdACeros_DevuelveBadRequestSinConsultarACourseAuthoring()
    {
        await factory.ResetAsync();
        factory.LessonSet.Publish(new LessonId(Guid.CreateVersion7()));

        var response = await MarcarAsync(Guid.Empty, Guid.CreateVersion7());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, factory.LessonSet.GetCount);
    }

    [Theory]
    [InlineData("""{"lessonId":"00000000-0000-0000-0000-000000000000"}""")]
    [InlineData("""{"lessonId":null}""")]
    [InlineData("""{}""")]
    [InlineData("""{"lessonId":"no-soy-un-guid"}""")]
    public async Task LessonIdInvalido_DevuelveBadRequestSinConsultarACourseAuthoring(string json)
    {
        await factory.ResetAsync();
        factory.LessonSet.Publish(new LessonId(Guid.CreateVersion7()));

        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await factory.CreateClientFor(Student)
            .PostAsync($"/api/v1/me/course-progress/{Guid.CreateVersion7()}/completed-lessons", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, factory.LessonSet.GetCount);
    }

    [Fact]
    public async Task CourseIdQueNoEsGuid_NoEncuentraRuta()
    {
        await factory.ResetAsync();

        var response = await factory.CreateClientFor(Student)
            .PostAsJsonAsync("/api/v1/me/course-progress/no-soy-un-guid/completed-lessons", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DosEscriturasSeguidas_ProducenDosConsultasAlConjunto()
    {
        var (course, first, _) = await PublicarCursoDeDosLecciones();

        await MarcarAsync(course, first);
        await MarcarAsync(course, first);

        Assert.Equal(2, factory.LessonSet.GetCount);
    }

    [Fact]
    public async Task Lista_DevuelveSoloLosProgresosDelActorYFiltraPorEstado()
    {
        var (course, first, second) = await PublicarCursoDeDosLecciones();

        await MarcarAsync(course, first);

        var enCurso = await LeerListaAsync("?status=InProgress");
        var completados = await LeerListaAsync("?status=Completed");

        Assert.Single(enCurso);
        Assert.Empty(completados);

        await MarcarAsync(course, second);

        Assert.Empty(await LeerListaAsync("?status=InProgress"));
        Assert.Single(await LeerListaAsync("?status=completed"));

        var ajeno = await factory.CreateClientFor(Guid.CreateVersion7())
            .GetFromJsonAsync<JsonElement>("/api/v1/me/course-progress");

        Assert.Empty(ajeno.EnumerateArray());
    }

    [Theory]
    [InlineData("?status=1")]
    [InlineData("?status=")]
    [InlineData("?status=Finalizado")]
    public async Task Lista_ConStatusInvalido_DevuelveBadRequest(string query)
    {
        await factory.ResetAsync();

        var response = await factory.CreateClientFor(Student)
            .GetAsync($"/api/v1/me/course-progress{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Detalle_SinProgreso_DevuelveNotFound()
    {
        await factory.ResetAsync();

        var response = await DetalleAsync(Guid.CreateVersion7());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Detalle_DeOtroEstudiante_NoDevuelveElProgresoAjeno()
    {
        var (course, first, _) = await PublicarCursoDeDosLecciones();

        await MarcarAsync(course, first);

        var ajeno = await factory.CreateClientFor(Guid.CreateVersion7())
            .GetAsync($"/api/v1/me/course-progress/{course}");

        Assert.Equal(HttpStatusCode.NotFound, ajeno.StatusCode);
    }

    [Fact]
    public async Task Health_RespondeConLaBaseArriba()
    {
        var response = await factory.CreateAnonymousClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Openapi_DescribeLasCuatroRutasDeNegocio()
    {
        var document = await factory.CreateAnonymousClient()
            .GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        var rutas = document.GetProperty("paths").EnumerateObject()
            .Select(path => path.Name)
            .Where(path => path.StartsWith("/api/", StringComparison.Ordinal))
            .OrderBy(path => path)
            .ToList();

        Assert.Equal(
            [
                "/api/v1/me/course-progress",
                "/api/v1/me/course-progress/{courseId}",
                "/api/v1/me/course-progress/{courseId}/completed-lessons",
                "/api/v1/me/course-progress/{courseId}/completion",
            ],
            rutas);
    }

    private async Task<(Guid Course, Guid First, Guid Second)> PublicarCursoDeDosLecciones()
    {
        await factory.ResetAsync();

        var first = new LessonId(Guid.CreateVersion7());
        var second = new LessonId(Guid.CreateVersion7());

        factory.LessonSet.Publish(first, second);

        return (Guid.CreateVersion7(), first.Value, second.Value);
    }

    private Task<HttpResponseMessage> MarcarAsync(Guid course, Guid lesson) =>
        factory.CreateClientFor(Student).PostAsJsonAsync(
            $"/api/v1/me/course-progress/{course}/completed-lessons",
            new { lessonId = lesson });

    private Task<HttpResponseMessage> ConfirmarAsync(Guid course) =>
        factory.CreateClientFor(Student)
            .PostAsync($"/api/v1/me/course-progress/{course}/completion", null);

    private Task<HttpResponseMessage> DetalleAsync(Guid course) =>
        factory.CreateClientFor(Student).GetAsync($"/api/v1/me/course-progress/{course}");

    private async Task<IReadOnlyList<JsonElement>> LeerListaAsync(string query)
    {
        var lista = await factory.CreateClientFor(Student)
            .GetFromJsonAsync<JsonElement>($"/api/v1/me/course-progress{query}");

        return [.. lista.EnumerateArray()];
    }

    private static IReadOnlyList<Guid> LessonIds(JsonElement body) =>
        [.. body.GetProperty("completedLessonIds").EnumerateArray().Select(id => id.GetGuid())];
}
