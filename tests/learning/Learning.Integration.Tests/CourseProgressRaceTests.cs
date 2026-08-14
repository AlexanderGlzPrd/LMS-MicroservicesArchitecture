using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Learning.Domain.Progress;
using Npgsql;
namespace Learning.Integration.Tests;

public sealed class CourseProgressRaceTests(LearningApiFactory factory) : IClassFixture<LearningApiFactory>
{
    private static readonly Guid Student = Guid.CreateVersion7();
    private static readonly DateTimeOffset Ganador = new(2026, 8, 14, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ColisionSobreLaClavePrimariaDelProgreso_TerminaEn200ConElAgregadoGanador()
    {
        await factory.ResetAsync();

        var course = Guid.CreateVersion7();
        var mia = new LessonId(Guid.CreateVersion7());
        var ajena = new LessonId(Guid.CreateVersion7());

        factory.LessonSet.Publish(mia, ajena);

        factory.RaceHook = async () =>
        {
            await InsertarProgresoAsync(course, CourseProgressStatus.InProgress);
            await InsertarLeccionAsync(course, ajena);
        };

        var response = await MarcarAsync(course, mia.Value);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Equal(
            new HashSet<Guid> { mia.Value, ajena.Value },
            new HashSet<Guid>(LessonIds(body)));

        Assert.Equal(Ganador, body.GetProperty("startedAt").GetDateTimeOffset());

        Assert.Equal(1, await factory.CountProgressAsync(Student, course));
        Assert.Equal(2, await factory.CountCompletedLessonsAsync(Student, course));
    }

    [Fact]
    public async Task ColisionSobreLaClavePrimariaDeLaLeccion_TerminaEnNoOpIdempotente()
    {
        await factory.ResetAsync();

        var course = Guid.CreateVersion7();
        var lesson = new LessonId(Guid.CreateVersion7());
        var otra = new LessonId(Guid.CreateVersion7());

        factory.LessonSet.Publish(lesson, otra);

        await InsertarProgresoAsync(course, CourseProgressStatus.InProgress);

        factory.RaceHook = () => InsertarLeccionAsync(course, lesson);

        var response = await MarcarAsync(course, lesson.Value);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([lesson.Value], LessonIds(body));

        Assert.Equal(1, await factory.CountProgressAsync(Student, course));
        Assert.Equal(1, await factory.CountCompletedLessonsAsync(Student, course));
    }

    [Fact]
    public async Task ColisionEnLaUltimaLeccion_SellaSobreElAgregadoGanador()
    {
        await factory.ResetAsync();

        var course = Guid.CreateVersion7();
        var mia = new LessonId(Guid.CreateVersion7());
        var ajena = new LessonId(Guid.CreateVersion7());

        factory.LessonSet.Publish(mia, ajena);

        factory.RaceHook = async () =>
        {
            await InsertarProgresoAsync(course, CourseProgressStatus.InProgress);
            await InsertarLeccionAsync(course, ajena);
        };

        var body = await (await MarcarAsync(course, mia.Value)).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(nameof(CourseProgressStatus.Completed), body.GetProperty("status").GetString());
        Assert.NotEqual(JsonValueKind.Null, body.GetProperty("completedAt").ValueKind);
    }

    private Task<HttpResponseMessage> MarcarAsync(Guid course, Guid lesson) =>
        factory.CreateClientFor(Student).PostAsJsonAsync(
            $"/api/v1/me/course-progress/{course}/completed-lessons",
            new { lessonId = lesson });

    private async Task InsertarProgresoAsync(Guid course, CourseProgressStatus status)
    {
        await using var connection = new NpgsqlConnection(factory.ConnectionString);
        await connection.OpenAsync(CancellationToken.None);

        await using var command = new NpgsqlCommand(
            "INSERT INTO course_progress (student_id, course_id, status, started_at, completed_at) "
            + "VALUES (@student, @course, @status, @startedAt, NULL)",
            connection);

        command.Parameters.AddWithValue("student", Student);
        command.Parameters.AddWithValue("course", course);
        command.Parameters.AddWithValue("status", status.ToString());
        command.Parameters.AddWithValue("startedAt", Ganador);

        await command.ExecuteNonQueryAsync(CancellationToken.None);
    }

    private async Task InsertarLeccionAsync(Guid course, LessonId lesson)
    {
        await using var connection = new NpgsqlConnection(factory.ConnectionString);
        await connection.OpenAsync(CancellationToken.None);

        await using var command = new NpgsqlCommand(
            "INSERT INTO completed_lessons (student_id, course_id, lesson_id, completed_at) "
            + "VALUES (@student, @course, @lesson, @completedAt)",
            connection);

        command.Parameters.AddWithValue("student", Student);
        command.Parameters.AddWithValue("course", course);
        command.Parameters.AddWithValue("lesson", lesson.Value);
        command.Parameters.AddWithValue("completedAt", Ganador);

        await command.ExecuteNonQueryAsync(CancellationToken.None);
    }

    private static IReadOnlyList<Guid> LessonIds(JsonElement body) =>
        [.. body.GetProperty("completedLessonIds").EnumerateArray().Select(id => id.GetGuid())];
}
