using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CourseAuthoring.Integration.Tests;

public sealed class CatalogLessonIdsApiTests(CourseAuthoringApiFactory factory)
    : IClassFixture<CourseAuthoringApiFactory>
{
    private static readonly Guid Owner = Guid.CreateVersion7();

    [Fact]
    public async Task CursoPublicado_DevuelveSusLeccionesEnOrdenDePosicion()
    {
        await factory.ResetAsync();

        var (courseId, lessonIds) = await PublishCourse(lessons: 3);

        var response = await factory.CreateAnonymousClient()
            .GetAsync($"/api/v1/catalog/courses/{courseId}/lesson-ids");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(courseId, body.GetProperty("courseId").GetGuid());
        Assert.Equal(lessonIds, ReadLessonIds(body));

        Assert.Equal(
            ["courseId", "lessonIds"],
            body.EnumerateObject().Select(property => property.Name).ToList());
    }

    [Fact]
    public async Task ElOrdenSigueALaPosicionPublicada_NoAlOrdenDeCreacion()
    {
        await factory.ResetAsync();

        var author = factory.CreateClientFor(Owner);
        var (courseId, lessonIds) = await PublishCourse(lessons: 3);

        IReadOnlyList<Guid> invertido = [.. lessonIds.Reverse()];

        var reorder = await author.PutAsJsonAsync(
            $"/api/v1/courses/{courseId}/lessons/order",
            new { lessonIds = invertido });

        reorder.EnsureSuccessStatusCode();

        var republish = await author.PostAsync($"/api/v1/courses/{courseId}/republish", null);
        republish.EnsureSuccessStatusCode();

        var body = await factory.CreateAnonymousClient()
            .GetFromJsonAsync<JsonElement>($"/api/v1/catalog/courses/{courseId}/lesson-ids");

        Assert.Equal(invertido, ReadLessonIds(body));
    }

    [Fact]
    public async Task EditarElContenidoDeTrabajo_NoCambiaLaRespuestaHastaRepublicar()
    {
        await factory.ResetAsync();

        var author = factory.CreateClientFor(Owner);
        var (courseId, publicadas) = await PublishCourse(lessons: 2);
        var publico = factory.CreateAnonymousClient();

        var nueva = await AddLesson(author, courseId, 3);

        var antesDeRepublicar = await publico
            .GetFromJsonAsync<JsonElement>($"/api/v1/catalog/courses/{courseId}/lesson-ids");

        Assert.Equal(publicadas, ReadLessonIds(antesDeRepublicar));
        Assert.DoesNotContain(nueva, ReadLessonIds(antesDeRepublicar));

        var republish = await author.PostAsync($"/api/v1/courses/{courseId}/republish", null);
        republish.EnsureSuccessStatusCode();

        var despues = await publico
            .GetFromJsonAsync<JsonElement>($"/api/v1/catalog/courses/{courseId}/lesson-ids");

        Assert.Equal([.. publicadas, nueva], ReadLessonIds(despues));
    }

    [Fact]
    public async Task CursoEnBorrador_YCursoInexistente_DevuelvenElMismoNotFound()
    {
        await factory.ResetAsync();

        var author = factory.CreateClientFor(Owner);
        var draftId = await CreateCourse(author, "Borrador");

        var publico = factory.CreateAnonymousClient();

        var borrador = await publico.GetAsync($"/api/v1/catalog/courses/{draftId}/lesson-ids");
        var inexistente = await publico
            .GetAsync($"/api/v1/catalog/courses/{Guid.CreateVersion7()}/lesson-ids");

        Assert.Equal(HttpStatusCode.NotFound, borrador.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.Equal("application/problem+json", borrador.Content.Headers.ContentType?.MediaType);
        Assert.Equal("application/problem+json", inexistente.Content.Headers.ContentType?.MediaType);

        var borradorBody = await borrador.Content.ReadFromJsonAsync<JsonElement>();
        var inexistenteBody = await inexistente.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            borradorBody.GetProperty("title").GetString(),
            inexistenteBody.GetProperty("title").GetString());
    }

    [Fact]
    public async Task NoExigeCabeceraDeInstructor()
    {
        await factory.ResetAsync();

        var (courseId, _) = await PublishCourse(lessons: 1);

        var response = await factory.CreateAnonymousClient()
            .GetAsync($"/api/v1/catalog/courses/{courseId}/lesson-ids");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static IReadOnlyList<Guid> ReadLessonIds(JsonElement body) =>
        [.. body.GetProperty("lessonIds").EnumerateArray().Select(id => id.GetGuid())];

    private async Task<(Guid CourseId, IReadOnlyList<Guid> LessonIds)> PublishCourse(int lessons)
    {
        var author = factory.CreateClientFor(Owner);
        var courseId = await CreateCourse(author, "Curso publicado");

        var lessonIds = new List<Guid>();

        for (var index = 1; index <= lessons; index++)
        {
            lessonIds.Add(await AddLesson(author, courseId, index));
        }

        var publish = await author.PostAsync($"/api/v1/courses/{courseId}/publish", null);
        publish.EnsureSuccessStatusCode();

        return (courseId, lessonIds);
    }

    private static async Task<Guid> AddLesson(HttpClient author, Guid courseId, int index)
    {
        var response = await author.PostAsJsonAsync($"/api/v1/courses/{courseId}/lessons",
            new
            {
                title = $"Leccion {index}",
                description = $"Descripcion {index}",
                videoUrl = $"https://videos.example.com/{Guid.CreateVersion7()}.mp4",
            });

        response.EnsureSuccessStatusCode();

        var lesson = await response.Content.ReadFromJsonAsync<JsonElement>();

        return lesson.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateCourse(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync("/api/v1/courses", new { title });
        response.EnsureSuccessStatusCode();

        var course = await response.Content.ReadFromJsonAsync<JsonElement>();

        return course.GetProperty("id").GetGuid();
    }
}
