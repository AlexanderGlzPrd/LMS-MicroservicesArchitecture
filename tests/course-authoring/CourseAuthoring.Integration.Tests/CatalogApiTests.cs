using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CourseAuthoring.Integration.Tests;

public sealed class CatalogApiTests(CourseAuthoringApiFactory factory)
    : IClassFixture<CourseAuthoringApiFactory>
{
    private static readonly Guid Owner = Guid.CreateVersion7();

    [Fact]
    public async Task Listado_RespondeSinCabeceraYConLosCuatroCamposDePaginacion()
    {
        await factory.ResetAsync();
        await PublishCourse("Publicado", lessons: 2);

        var response = await factory.CreateAnonymousClient().GetAsync("/api/v1/catalog/courses");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(1, page.GetProperty("page").GetInt32());
        Assert.Equal(20, page.GetProperty("pageSize").GetInt32());
        Assert.Equal(1, page.GetProperty("totalCount").GetInt32());

        var item = Assert.Single(page.GetProperty("items").EnumerateArray());

        Assert.Equal("Publicado", item.GetProperty("title").GetString());
        Assert.Equal(Owner, item.GetProperty("instructorId").GetGuid());
        Assert.Equal(2, item.GetProperty("lessonCount").GetInt32());
        Assert.NotEqual(JsonValueKind.Null, item.GetProperty("publishedAt").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, item.GetProperty("publishedContentUpdatedAt").ValueKind);

        Assert.False(item.TryGetProperty("lessons", out _));
    }

    [Fact]
    public async Task Listado_NoIncluyeBorradores_YSuDetalleEsNotFound()
    {
        await factory.ResetAsync();
        await PublishCourse("Publicado", lessons: 1);

        var author = factory.CreateClientFor(Owner);
        var draftId = await CreateCourse(author, "Borrador");

        var publico = factory.CreateAnonymousClient();
        var page = await publico.GetFromJsonAsync<JsonElement>("/api/v1/catalog/courses");

        Assert.Equal(1, page.GetProperty("totalCount").GetInt32());
        Assert.DoesNotContain(
            page.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == draftId);

        var detail = await publico.GetAsync($"/api/v1/catalog/courses/{draftId}");

        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
        Assert.Equal("application/problem+json", detail.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task DetalleDeCursoInexistente_DevuelveNotFound()
    {
        var detail = await factory.CreateAnonymousClient()
            .GetAsync($"/api/v1/catalog/courses/{Guid.CreateVersion7()}");

        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?page=-1")]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=101")]
    public async Task PaginacionFueraDeRango_DevuelveBadRequest(string query)
    {
        var response = await factory.CreateAnonymousClient()
            .GetAsync($"/api/v1/catalog/courses{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Paginacion_RecortaLosElementosYConservaElTotal()
    {
        await factory.ResetAsync();

        for (var index = 0; index < 3; index++)
        {
            await PublishCourse($"Curso {index}", lessons: 1);
        }

        var page = await factory.CreateAnonymousClient()
            .GetFromJsonAsync<JsonElement>("/api/v1/catalog/courses?page=2&pageSize=2");

        Assert.Equal(2, page.GetProperty("page").GetInt32());
        Assert.Equal(2, page.GetProperty("pageSize").GetInt32());
        Assert.Equal(3, page.GetProperty("totalCount").GetInt32());
        Assert.Single(page.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Republicar_NoDevuelveElCursoAlPrincipioDelListado()
    {
        await factory.ResetAsync();

        var primero = await PublishCourse("Primero", lessons: 1);
        var segundo = await PublishCourse("Segundo", lessons: 1);

        var publico = factory.CreateAnonymousClient();

        // Orden published_at DESC: el ultimo publicado va primero.
        Assert.Equal([segundo, primero], await CatalogIds(publico));

        var author = factory.CreateClientFor(Owner);
        await author.PatchAsJsonAsync($"/api/v1/courses/{primero}", new { title = "Primero v2" });
        var republish = await author.PostAsync($"/api/v1/courses/{primero}/republish", null);

        Assert.Equal(HttpStatusCode.OK, republish.StatusCode);

        Assert.Equal([segundo, primero], await CatalogIds(publico));
    }

    [Fact]
    public async Task DetallePublico_NoExponeContenidoDeTrabajoNiCambiosPendientes()
    {
        await factory.ResetAsync();

        var courseId = await PublishCourse("Titulo publicado", lessons: 1);

        var author = factory.CreateClientFor(Owner);
        await author.PatchAsJsonAsync($"/api/v1/courses/{courseId}", new { title = "Titulo de trabajo" });

        var detail = await factory.CreateAnonymousClient()
            .GetFromJsonAsync<JsonElement>($"/api/v1/catalog/courses/{courseId}");

        Assert.Equal("Titulo publicado", detail.GetProperty("title").GetString());

        var propertyNames = detail.EnumerateObject().Select(property => property.Name).ToList();

        Assert.Equal(
            ["id", "title", "instructorId", "publishedAt", "publishedContentUpdatedAt", "lessons"],
            propertyNames);
    }

    private async Task<Guid> PublishCourse(string title, int lessons)
    {
        var author = factory.CreateClientFor(Owner);
        var courseId = await CreateCourse(author, title);

        for (var index = 1; index <= lessons; index++)
        {
            var response = await author.PostAsJsonAsync($"/api/v1/courses/{courseId}/lessons",
                new
                {
                    title = $"Leccion {index}",
                    description = $"Descripcion {index}",
                    videoUrl = $"https://videos.example.com/{Guid.CreateVersion7()}.mp4",
                });

            response.EnsureSuccessStatusCode();
        }

        var publish = await author.PostAsync($"/api/v1/courses/{courseId}/publish", null);
        publish.EnsureSuccessStatusCode();

        return courseId;
    }

    private static async Task<Guid> CreateCourse(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync("/api/v1/courses", new { title });
        response.EnsureSuccessStatusCode();

        var course = await response.Content.ReadFromJsonAsync<JsonElement>();

        return course.GetProperty("id").GetGuid();
    }

    private static async Task<IReadOnlyList<Guid>> CatalogIds(HttpClient client)
    {
        var page = await client.GetFromJsonAsync<JsonElement>("/api/v1/catalog/courses");

        return [.. page.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid())];
    }
}
