using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Enrollments.Application.Abstractions;

namespace Enrollments.Integration.Tests;

public sealed class EnrollmentApiTests(EnrollmentsApiFactory factory)
    : IClassFixture<EnrollmentsApiFactory>
{
    [Fact]
    public async Task Matricula_NuevaDevuelve201ConLocationYTipoFree()
    {
        await factory.ResetAsync();
        var student = Guid.CreateVersion7();
        var course = Guid.CreateVersion7();

        var response = await Enroll(student, course);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/v1/me/enrollments/{course}", response.Headers.Location?.ToString());

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(student, body.GetProperty("studentId").GetGuid());
        Assert.Equal(course, body.GetProperty("courseId").GetGuid());
        Assert.Equal("Free", body.GetProperty("type").GetString());

        Assert.Equal(1, await factory.CountEnrollmentsAsync(student, course));
    }

    [Fact]
    public async Task Matricula_RepetidaDevuelve200ConElMismoIdSinLocationYSinConsultarElCatalogo()
    {
        await factory.ResetAsync();
        var student = Guid.CreateVersion7();
        var course = Guid.CreateVersion7();

        var first = await Enroll(student, course);
        var checksAfterFirst = factory.CourseAvailability.CheckCount;

        var second = await Enroll(student, course);

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Null(second.Headers.Location);
        Assert.Equal(await IdOf(first), await IdOf(second));

        Assert.Equal(checksAfterFirst, factory.CourseAvailability.CheckCount);
        Assert.Equal(1, await factory.CountEnrollmentsAsync(student, course));
    }

    [Fact]
    public async Task Matricula_ConCursoNoDisponibleDevuelve422ProblemJson()
    {
        await factory.ResetAsync();
        factory.CourseAvailability.Result = CourseAvailability.NotAvailable;

        var student = Guid.CreateVersion7();
        var course = Guid.CreateVersion7();

        var response = await Enroll(student, course);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, await factory.CountEnrollmentsAsync(student, course));
    }

    [Fact]
    public async Task Matricula_ConDisponibilidadDesconocidaDevuelve503ConRetryAfterDeConfiguracion()
    {
        await factory.ResetAsync();
        factory.CourseAvailability.Result = CourseAvailability.Unknown;

        var student = Guid.CreateVersion7();
        var course = Guid.CreateVersion7();

        var response = await Enroll(student, course);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        // El numero sale de Services:CourseAuthoring:RetryAfterSeconds, no del codigo.
        Assert.Equal(
            EnrollmentsApiFactory.RetryAfterSeconds,
            response.Headers.RetryAfter?.Delta?.TotalSeconds);

        Assert.Equal(0, await factory.CountEnrollmentsAsync(student, course));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"courseId\":null}")]
    [InlineData("{\"courseId\":\"no-es-un-guid\"}")]
    [InlineData("{\"courseId\":\"00000000-0000-0000-0000-000000000000\"}")]
    public async Task Matricula_ConCuerpoInvalidoDevuelve400SinConsultarElCatalogo(string body)
    {
        await factory.ResetAsync();

        var response = await factory.CreateClientFor(Guid.CreateVersion7()).PostAsync(
            "/api/v1/enrollments",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, factory.CourseAvailability.CheckCount);
    }

    [Fact]
    public async Task Matricula_SinCabeceraDeEstudianteDevuelve400()
    {
        await factory.ResetAsync();

        var response = await factory.CreateAnonymousClient().PostAsJsonAsync(
            "/api/v1/enrollments",
            new { courseId = Guid.CreateVersion7() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Matricula_ConEstudianteACerosDevuelve400YNo500()
    {
        await factory.ResetAsync();

        var response = await factory.CreateClientFor(Guid.Empty).PostAsJsonAsync(
            "/api/v1/enrollments",
            new { courseId = Guid.CreateVersion7() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Listado_DevuelveSoloLasDelActorComoArrayPlanoOrdenado()
    {
        await factory.ResetAsync();
        var student = Guid.CreateVersion7();
        var other = Guid.CreateVersion7();

        var older = Guid.CreateVersion7();
        var newer = Guid.CreateVersion7();

        await Enroll(student, older);
        await Enroll(student, newer);
        await Enroll(other, Guid.CreateVersion7());

        var body = await factory.CreateClientFor(student)
            .GetFromJsonAsync<JsonElement>("/api/v1/me/enrollments");

        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Assert.Equal(
            [newer, older],
            body.EnumerateArray().Select(item => item.GetProperty("courseId").GetGuid()));
    }

    [Fact]
    public async Task Listado_IgnoraLosParametrosDePaginacion()
    {
        await factory.ResetAsync();
        var student = Guid.CreateVersion7();

        await Enroll(student, Guid.CreateVersion7());
        await Enroll(student, Guid.CreateVersion7());

        var client = factory.CreateClientFor(student);

        var plain = await client.GetFromJsonAsync<JsonElement>("/api/v1/me/enrollments");
        var paged = await client.GetFromJsonAsync<JsonElement>(
            "/api/v1/me/enrollments?page=2&pageSize=1");

        Assert.Equal(2, paged.GetArrayLength());
        Assert.Equal(plain.ToString(), paged.ToString());
    }

    [Fact]
    public async Task ConsultaPorCurso_DevuelveLaMatriculaSiExiste()
    {
        await factory.ResetAsync();
        var student = Guid.CreateVersion7();
        var course = Guid.CreateVersion7();

        var created = await Enroll(student, course);

        var response = await factory.CreateClientFor(student)
            .GetAsync($"/api/v1/me/enrollments/{course}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(await IdOf(created), await IdOf(response));
    }

    [Fact]
    public async Task ConsultaPorCurso_SinMatriculaDelActorDevuelve404ProblemJson()
    {
        await factory.ResetAsync();
        var student = Guid.CreateVersion7();
        var course = Guid.CreateVersion7();

        // La matricula existe, pero es de otro estudiante.
        await Enroll(Guid.CreateVersion7(), course);

        var response = await factory.CreateClientFor(student)
            .GetAsync($"/api/v1/me/enrollments/{course}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Openapi_DescribeLasTresRutasDeNegocioBajoApiV1()
    {
        var document = await factory.CreateAnonymousClient()
            .GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        var paths = document.GetProperty("paths").EnumerateObject()
            .Select(path => path.Name)
            .ToList();

        Assert.Contains("/api/v1/enrollments", paths);
        Assert.Contains("/api/v1/me/enrollments", paths);
        Assert.Contains("/api/v1/me/enrollments/{courseId}", paths);
        Assert.All(paths, path => Assert.StartsWith("/api/v1", path));
    }

    [Fact]
    public async Task Health_RespondeSinDependerDeCourseAuthoring()
    {
        await factory.ResetAsync();
        factory.CourseAvailability.Result = CourseAvailability.Unknown;

        var response = await factory.CreateAnonymousClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private Task<HttpResponseMessage> Enroll(Guid student, Guid course) =>
        factory.CreateClientFor(student).PostAsJsonAsync(
            "/api/v1/enrollments",
            new { courseId = course });

    private static async Task<Guid> IdOf(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
}
