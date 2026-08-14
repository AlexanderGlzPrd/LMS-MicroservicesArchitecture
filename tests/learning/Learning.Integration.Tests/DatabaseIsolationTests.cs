using Npgsql;
using Testcontainers.PostgreSql;
namespace Learning.Integration.Tests;

public sealed class DatabaseIsolationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17")
        .WithResourceMapping(
            new FileInfo(ScriptPath("01-course-authoring.sql")),
            "/docker-entrypoint-initdb.d/")
        .WithResourceMapping(
            new FileInfo(ScriptPath("02-enrollment.sql")),
            "/docker-entrypoint-initdb.d/")
        .WithResourceMapping(
            new FileInfo(ScriptPath("03-learning.sql")),
            "/docker-entrypoint-initdb.d/")
        .Build();

    public Task InitializeAsync() => container.StartAsync();

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    [Theory]
    [InlineData("course_authoring_user", "course_authoring_dev", "enrollment")]
    [InlineData("course_authoring_user", "course_authoring_dev", "learning")]
    [InlineData("enrollment_user", "enrollment_dev", "course_authoring")]
    [InlineData("enrollment_user", "enrollment_dev", "learning")]
    [InlineData("learning_user", "learning_dev", "course_authoring")]
    [InlineData("learning_user", "learning_dev", "enrollment")]
    public async Task UsuarioDeServicio_NoAlcanzaLaBaseDeOtroServicio(
        string username,
        string password,
        string foreignDatabase)
    {
        await using var connection = new NpgsqlConnection(
            ConnectionStringFor(username, password, foreignDatabase));

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => connection.OpenAsync(CancellationToken.None));

        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, exception.SqlState);
        Assert.Contains("permission denied for database", exception.MessageText);
    }

    [Theory]
    [InlineData("course_authoring_user", "course_authoring_dev", "course_authoring")]
    [InlineData("enrollment_user", "enrollment_dev", "enrollment")]
    [InlineData("learning_user", "learning_dev", "learning")]
    public async Task UsuarioDeServicio_SiAlcanzaSuPropiaBase(
        string username,
        string password,
        string ownDatabase)
    {
        await using var connection = new NpgsqlConnection(
            ConnectionStringFor(username, password, ownDatabase));

        await connection.OpenAsync(CancellationToken.None);

        await using var command = new NpgsqlCommand("SELECT current_database()", connection);

        Assert.Equal(ownDatabase, await command.ExecuteScalarAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData("course_authoring_user")]
    [InlineData("enrollment_user")]
    [InlineData("learning_user")]
    public async Task UsuarioDeServicio_NoTienePrivilegiosAdministrativos(string username)
    {
        await using var connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync(CancellationToken.None);

        await using var command = new NpgsqlCommand(
            "SELECT rolsuper, rolcreatedb, rolcreaterole FROM pg_roles WHERE rolname = @role",
            connection);

        command.Parameters.AddWithValue("role", username);

        await using var reader = await command.ExecuteReaderAsync(CancellationToken.None);

        Assert.True(await reader.ReadAsync(CancellationToken.None));
        Assert.False(reader.GetBoolean(0));
        Assert.False(reader.GetBoolean(1));
        Assert.False(reader.GetBoolean(2));
    }

    [Theory]
    [InlineData("course_authoring")]
    [InlineData("enrollment")]
    [InlineData("learning")]
    public async Task Base_NoConcedeConnectAPublic(string database)
    {
        await using var connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync(CancellationToken.None);

        await using var command = new NpgsqlCommand(
            "SELECT has_database_privilege('public', @database, 'CONNECT')",
            connection);

        command.Parameters.AddWithValue("database", database);

        Assert.Equal(false, await command.ExecuteScalarAsync(CancellationToken.None));
    }

    private string ConnectionStringFor(string username, string password, string database) =>
        new NpgsqlConnectionStringBuilder(container.GetConnectionString())
        {
            Database = database,
            Username = username,
            Password = password,
        }.ConnectionString;

    private static string ScriptPath(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "LMS.sln")))
        {
            directory = directory.Parent;
        }

        return directory is null
            ? throw new InvalidOperationException("No se ha encontrado la raiz del repositorio.")
            : Path.Combine(directory.FullName, "deploy", "postgres", "init", fileName);
    }
}
