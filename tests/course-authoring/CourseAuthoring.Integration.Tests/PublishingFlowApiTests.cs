using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CourseAuthoring.Integration.Tests;

public sealed class PublishingFlowApiTests(CourseAuthoringApiFactory factory)
    : IClassFixture<CourseAuthoringApiFactory>
{
    private static readonly Guid Owner = Guid.CreateVersion7();
    private static readonly Guid Intruder = Guid.CreateVersion7();

    [Fact]
    public async Task FlujoCompleto_DeBorradorACatalogoActualizado()
    {
        await factory.ResetAsync();

        var author = factory.CreateClientFor(Owner);
        var publico = factory.CreateAnonymousClient();

        // 1. Crear.
        var create = await author.PostAsJsonAsync("/api/v1/courses",
            new { title = "Microservicios con .NET" });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var course = await create.Content.ReadFromJsonAsync<JsonElement>();
        var courseId = course.GetProperty("id").GetGuid();

        Assert.Equal($"/api/v1/courses/{courseId}", create.Headers.Location?.AbsolutePath);
        Assert.Equal("Draft", course.GetProperty("status").GetString());
        Assert.Empty(course.GetProperty("lessons").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, course.GetProperty("publishedAt").ValueKind);

        var first = await AddLesson(author, courseId, "Introduccion");
        await AddLesson(author, courseId, "Bounded Contexts");

        var publish = await author.PostAsync($"/api/v1/courses/{courseId}/publish", null);

        Assert.Equal(HttpStatusCode.OK, publish.StatusCode);

        var published = await publish.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Published", published.GetProperty("status").GetString());

        var publishedAt = ToMicroseconds(published.GetProperty("publishedAt").GetDateTimeOffset());

        var catalogItem = Assert.Single(await CatalogItems(publico));

        Assert.Equal(courseId, catalogItem.GetProperty("id").GetGuid());
        Assert.Equal("Microservicios con .NET", catalogItem.GetProperty("title").GetString());
        Assert.Equal(2, catalogItem.GetProperty("lessonCount").GetInt32());

        var rename = await author.PatchAsJsonAsync($"/api/v1/courses/{courseId}",
            new { title = "Microservicios con .NET 10" });
        Assert.Equal(HttpStatusCode.OK, rename.StatusCode);

        var update = await author.PutAsJsonAsync($"/api/v1/courses/{courseId}/lessons/{first}",
            new
            {
                title = "Introduccion revisada",
                description = "Que es un microservicio",
                videoUrl = "https://cdn.example.com/1-v2.mp4",
            });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        await AddLesson(author, courseId, "Outbox");

        var beforeRepublish = Assert.Single(await CatalogItems(publico));

        Assert.Equal("Microservicios con .NET", beforeRepublish.GetProperty("title").GetString());
        Assert.Equal(2, beforeRepublish.GetProperty("lessonCount").GetInt32());
        Assert.Equal(publishedAt,
            ToMicroseconds(beforeRepublish.GetProperty("publishedContentUpdatedAt").GetDateTimeOffset()));

        var detailBefore = await publico.GetFromJsonAsync<JsonElement>(
            $"/api/v1/catalog/courses/{courseId}");

        Assert.Equal("Microservicios con .NET", detailBefore.GetProperty("title").GetString());
        Assert.Equal(["Introduccion", "Bounded Contexts"], Titles(detailBefore));

        var republish = await author.PostAsync($"/api/v1/courses/{courseId}/republish", null);
        var republished = await republish.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, republish.StatusCode);
        Assert.True(republished.GetProperty("changed").GetBoolean());

        var afterRepublish = Assert.Single(await CatalogItems(publico));

        Assert.Equal("Microservicios con .NET 10", afterRepublish.GetProperty("title").GetString());
        Assert.Equal(3, afterRepublish.GetProperty("lessonCount").GetInt32());
        Assert.Equal(publishedAt,
            ToMicroseconds(afterRepublish.GetProperty("publishedAt").GetDateTimeOffset()));
        Assert.True(afterRepublish.GetProperty("publishedContentUpdatedAt").GetDateTimeOffset() > publishedAt);

        var detailAfter = await publico.GetFromJsonAsync<JsonElement>(
            $"/api/v1/catalog/courses/{courseId}");

        Assert.Equal(["Introduccion revisada", "Bounded Contexts", "Outbox"], Titles(detailAfter));
        Assert.Equal("https://cdn.example.com/1-v2.mp4",
            detailAfter.GetProperty("lessons")[0].GetProperty("videoUrl").GetString());

        Assert.Equal(first, detailAfter.GetProperty("lessons")[0].GetProperty("id").GetGuid());

        var noop = await author.PostAsync($"/api/v1/courses/{courseId}/republish", null);
        var noopBody = await noop.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, noop.StatusCode);
        Assert.False(noopBody.GetProperty("changed").GetBoolean());
    }

    [Fact]
    public async Task InstructorAjeno_RecibeForbiddenEnLecturaYEnComandos()
    {
        await factory.ResetAsync();

        var author = factory.CreateClientFor(Owner);
        var intruder = factory.CreateClientFor(Intruder);

        var courseId = await CreateCourse(author, "Curso del propietario");
        await AddLesson(author, courseId, "Uno");

        var read = await intruder.GetAsync($"/api/v1/courses/{courseId}");
        Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);
        Assert.Equal("application/problem+json", read.Content.Headers.ContentType?.MediaType);

        var rename = await intruder.PatchAsJsonAsync($"/api/v1/courses/{courseId}",
            new { title = "Secuestrado" });
        Assert.Equal(HttpStatusCode.Forbidden, rename.StatusCode);

        var addLesson = await intruder.PostAsJsonAsync($"/api/v1/courses/{courseId}/lessons",
            new { title = "T", description = "D", videoUrl = "https://videos.example.com/x.mp4" });
        Assert.Equal(HttpStatusCode.Forbidden, addLesson.StatusCode);

        var publish = await intruder.PostAsync($"/api/v1/courses/{courseId}/publish", null);
        Assert.Equal(HttpStatusCode.Forbidden, publish.StatusCode);

        // El curso quedo intacto.
        var detail = await author.GetFromJsonAsync<JsonElement>($"/api/v1/courses/{courseId}");
        Assert.Equal("Curso del propietario", detail.GetProperty("title").GetString());
        Assert.Equal("Draft", detail.GetProperty("status").GetString());
    }

    [Fact]
    public async Task PublicarSinLecciones_DevuelveUnprocessableEntity()
    {
        await factory.ResetAsync();

        var author = factory.CreateClientFor(Owner);
        var courseId = await CreateCourse(author, "Sin lecciones");

        var publish = await author.PostAsync($"/api/v1/courses/{courseId}/publish", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, publish.StatusCode);
        Assert.Equal("application/problem+json", publish.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PublicarDosVeces_YRepublicarUnBorrador_DevuelvenConflict()
    {
        await factory.ResetAsync();

        var author = factory.CreateClientFor(Owner);

        var draftId = await CreateCourse(author, "Borrador");
        await AddLesson(author, draftId, "Uno");

        var republishDraft = await author.PostAsync($"/api/v1/courses/{draftId}/republish", null);
        Assert.Equal(HttpStatusCode.Conflict, republishDraft.StatusCode);

        await author.PostAsync($"/api/v1/courses/{draftId}/publish", null);

        var publishAgain = await author.PostAsync($"/api/v1/courses/{draftId}/publish", null);
        Assert.Equal(HttpStatusCode.Conflict, publishAgain.StatusCode);
        Assert.Equal("application/problem+json", publishAgain.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ReordenarConListaIncompleta_DevuelveUnprocessableEntity()
    {
        await factory.ResetAsync();

        var author = factory.CreateClientFor(Owner);
        var courseId = await CreateCourse(author, "Curso");
        var first = await AddLesson(author, courseId, "Uno");
        var second = await AddLesson(author, courseId, "Dos");

        var incomplete = await author.PutAsJsonAsync($"/api/v1/courses/{courseId}/lessons/order",
            new { lessonIds = new[] { first } });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, incomplete.StatusCode);
        Assert.Equal("application/problem+json", incomplete.Content.Headers.ContentType?.MediaType);

        var valid = await author.PutAsJsonAsync($"/api/v1/courses/{courseId}/lessons/order",
            new { lessonIds = new[] { second, first } });

        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);

        var reordered = await valid.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(["Dos", "Uno"],
            reordered.EnumerateArray().Select(lesson => lesson.GetProperty("title").GetString()));
    }

    [Theory]
    [InlineData("", "Descripcion", "https://videos.example.com/1.mp4")]
    [InlineData("Titulo", "", "https://videos.example.com/1.mp4")]
    [InlineData("Titulo", "Descripcion", "/videos/relativa.mp4")]
    public async Task DatosDeEntradaInvalidos_DevuelvenBadRequestYNoUnprocessableEntity(
        string title,
        string description,
        string videoUrl)
    {
        await factory.ResetAsync();

        var author = factory.CreateClientFor(Owner);
        var courseId = await CreateCourse(author, "Curso");

        var response = await author.PostAsJsonAsync($"/api/v1/courses/{courseId}/lessons",
            new { title, description, videoUrl });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SinCabeceraDeInstructor_LosComandosDeAutoriaDevuelvenBadRequest()
    {
        await factory.ResetAsync();

        var response = await factory.CreateAnonymousClient()
            .PostAsJsonAsync("/api/v1/courses", new { title = "Sin cabecera" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListadoDelInstructor_DevuelveResumenesSoloDelActor()
    {
        await factory.ResetAsync();

        var author = factory.CreateClientFor(Owner);
        var other = factory.CreateClientFor(Intruder);

        var courseId = await CreateCourse(author, "Del propietario");
        await AddLesson(author, courseId, "Uno");
        await author.PostAsync($"/api/v1/courses/{courseId}/publish", null);

        await CreateCourse(other, "De otro instructor");

        var summaries = await author.GetFromJsonAsync<JsonElement>("/api/v1/courses");
        var summary = Assert.Single(summaries.EnumerateArray());

        Assert.Equal(courseId, summary.GetProperty("id").GetGuid());
        Assert.Equal("Del propietario", summary.GetProperty("title").GetString());
        Assert.Equal("Published", summary.GetProperty("status").GetString());
        Assert.NotEqual(JsonValueKind.Null, summary.GetProperty("publishedAt").ValueKind);

        Assert.False(summary.TryGetProperty("lessons", out _));
        Assert.False(summary.TryGetProperty("instructorId", out _));
    }

    [Fact]
    public async Task RutasSinVersion_NoExisten_YLasDeInfraestructuraSiguenSinVersionar()
    {
        var author = factory.CreateClientFor(Owner);

        var unversioned = await author.PostAsJsonAsync("/courses", new { title = "Sin version" });
        Assert.Equal(HttpStatusCode.NotFound, unversioned.StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await author.GetAsync("/health")).StatusCode);

        var openApi = await factory.CreateAnonymousClient().GetFromJsonAsync<JsonElement>(
            "/openapi/v1.json");

        var paths = openApi.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/v1/courses/{id}/lessons", out _));
        Assert.True(paths.TryGetProperty("/api/v1/courses/{id}/publish", out _));
        Assert.True(paths.TryGetProperty("/api/v1/courses/{id}/republish", out _));
        Assert.True(paths.TryGetProperty("/api/v1/catalog/courses", out _));
        Assert.True(paths.TryGetProperty("/api/v1/catalog/courses/{id}", out _));
    }

    private static async Task<Guid> CreateCourse(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync("/api/v1/courses", new { title });
        response.EnsureSuccessStatusCode();

        var course = await response.Content.ReadFromJsonAsync<JsonElement>();

        return course.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> AddLesson(HttpClient client, Guid courseId, string title)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/courses/{courseId}/lessons",
            new
            {
                title,
                description = $"Descripcion de {title}",
                videoUrl = $"https://videos.example.com/{Guid.CreateVersion7()}.mp4",
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var lesson = await response.Content.ReadFromJsonAsync<JsonElement>();

        return lesson.GetProperty("id").GetGuid();
    }

    private static async Task<IReadOnlyList<JsonElement>> CatalogItems(HttpClient client)
    {
        var page = await client.GetFromJsonAsync<JsonElement>("/api/v1/catalog/courses");

        return [.. page.GetProperty("items").EnumerateArray()];
    }

    private static DateTimeOffset ToMicroseconds(DateTimeOffset value) =>
        new(value.Ticks - (value.Ticks % TimeSpan.TicksPerMicrosecond), value.Offset);

    private static IEnumerable<string?> Titles(JsonElement detail) =>
        detail.GetProperty("lessons").EnumerateArray()
            .Select(lesson => lesson.GetProperty("title").GetString());
}
