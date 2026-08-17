using Certification.Application.Abstractions;
using Certification.Domain.Certificates;
using Certification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace Certification.Infrastructure.Issuance;
internal sealed class CertificateIssuanceDispatcher(
    IServiceScopeFactory scopeFactory,
    IOptions<CertificateIssuanceOptions> options,
    TimeProvider timeProvider,
    ILogger<CertificateIssuanceDispatcher> logger) : BackgroundService
{
    private const int MaxLastErrorLength = 2000;

    internal const string CourseTitleNotFound = "CourseTitleNotFound";
    internal const string CourseTitleUnavailable = "CourseTitleUnavailable";
    internal const string StudentNameNotFound = "StudentNameNotFound";
    internal const string StudentNameUnavailable = "StudentNameUnavailable";
    internal const string ContradictoryCompletedAt = "ContradictoryCompletedAt";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(options.Value.PollingIntervalSeconds));

        try
        {
            do
            {
                try
                {
                    await IssueBatchAsync(stoppingToken);
                }
                catch (Exception exception) when (
                    exception is not OperationCanceledException
                    || !stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(exception, "Fallo inesperado en el ciclo de emision.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    internal async Task IssueBatchAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();

        var provider = scope.ServiceProvider;
        var database = provider.GetRequiredService<CertificationDbContext>();
        var courseCatalog = provider.GetRequiredService<ICourseCatalog>();
        var studentDirectory = provider.GetRequiredService<IStudentDirectory>();

        var pending = await database.PendingCertificateIssuances
            .AsNoTracking()
            .OrderBy(issuance => issuance.CreatedAt)
            .ThenBy(issuance => issuance.StudentId)
            .ThenBy(issuance => issuance.CourseId)
            .Take(options.Value.BatchSize)
            .ToListAsync(stoppingToken);

        foreach (var issuance in pending)
        {
            await IssueOneAsync(
                database, courseCatalog, studentDirectory, issuance, stoppingToken);
        }
    }

    private async Task IssueOneAsync(
        CertificationDbContext database,
        ICourseCatalog courseCatalog,
        IStudentDirectory studentDirectory,
        PendingCertificateIssuance issuance,
        CancellationToken stoppingToken)
    {
        var title = await courseCatalog.GetTitleAsync(issuance.CourseId, stoppingToken);
        var name = await studentDirectory.GetDisplayNameAsync(issuance.StudentId, stoppingToken);

        var unresolved = DescribeUnresolved(title, name);

        if (unresolved is not null)
        {
            await RecordFailedAttemptAsync(database, issuance, unresolved);

            return;
        }

        try
        {
            database.Certificates.Add(Certificate.Issue(
                new CertificateId(Guid.CreateVersion7()),
                issuance.StudentId,
                issuance.CourseId,
                name.DisplayName,
                title.Title,
                issuance.CompletedAt,
                timeProvider.GetUtcNow(),
                options.Value.Issuer));

            database.PendingCertificateIssuances.Remove(
                database.PendingCertificateIssuances.Attach(issuance).Entity);

            await database.SaveChangesAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DbUpdateException exception) when (IsDuplicateCertificate(exception))
        {
            database.ChangeTracker.Clear();

            await ResolveDuplicateAsync(database, issuance);
        }
        catch (Exception exception)
        {
            database.ChangeTracker.Clear();

            logger.LogError(
                exception,
                "Fallo al emitir el certificado de ({StudentId}, {CourseId}).",
                issuance.StudentId,
                issuance.CourseId);

            await RecordFailedAttemptAsync(
                database, issuance, $"{exception.GetType().FullName}: {exception.Message}");
        }
    }

    private static string? DescribeUnresolved(CourseTitleLookup title, StudentDirectoryEntry name)
    {
        if (title.Status == CourseTitleStatus.NotFound)
        {
            return CourseTitleNotFound;
        }

        if (title.Status == CourseTitleStatus.Unavailable)
        {
            return CourseTitleUnavailable;
        }

        if (name.Status == StudentDirectoryStatus.NotFound)
        {
            return StudentNameNotFound;
        }

        return name.Status == StudentDirectoryStatus.Unavailable
            ? StudentNameUnavailable
            : null;
    }

    private async Task ResolveDuplicateAsync(
        CertificationDbContext database,
        PendingCertificateIssuance issuance)
    {
        using var diagnostics = new CancellationTokenSource(
            TimeSpan.FromSeconds(options.Value.DiagnosticsTimeoutSeconds));

        try
        {
            var existing = await database.Certificates
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    certificate => certificate.StudentId == issuance.StudentId
                        && certificate.CourseId == issuance.CourseId,
                    diagnostics.Token);

            if (existing is null)
            {
                return;
            }

            if (existing.CompletedAt == issuance.CompletedAt)
            {
                database.PendingCertificateIssuances.Remove(
                    database.PendingCertificateIssuances.Attach(issuance).Entity);

                await database.SaveChangesAsync(diagnostics.Token);

                return;
            }

            await RecordFailedAttemptAsync(
                database,
                issuance,
                $"{ContradictoryCompletedAt}: el certificado existente sella " +
                $"'{existing.CompletedAt:O}' y la pendiente afirma '{issuance.CompletedAt:O}'.");
        }
        catch (Exception exception)
        {
            database.ChangeTracker.Clear();

            logger.LogError(
                exception,
                "No se pudo resolver el certificado duplicado de ({StudentId}, {CourseId}).",
                issuance.StudentId,
                issuance.CourseId);
        }
    }

    private async Task RecordFailedAttemptAsync(
        CertificationDbContext database,
        PendingCertificateIssuance issuance,
        string reason)
    {
        using var diagnostics = new CancellationTokenSource(
            TimeSpan.FromSeconds(options.Value.DiagnosticsTimeoutSeconds));

        try
        {
            var row = await database.PendingCertificateIssuances.FirstOrDefaultAsync(
                pending => pending.StudentId == issuance.StudentId
                    && pending.CourseId == issuance.CourseId,
                diagnostics.Token);

            if (row is null)
            {
                return;
            }

            row.AttemptCount++;
            row.LastError = Truncate(reason);
            row.LastAttemptAt = timeProvider.GetUtcNow();

            await database.SaveChangesAsync(diagnostics.Token);
        }
        catch (Exception exception)
        {
            database.ChangeTracker.Clear();

            logger.LogError(
                exception,
                "No se pudo registrar el intento fallido de ({StudentId}, {CourseId}).",
                issuance.StudentId,
                issuance.CourseId);
        }
    }

    private static bool IsDuplicateCertificate(DbUpdateException exception) =>
        exception.InnerException is Npgsql.PostgresException
        {
            SqlState: Npgsql.PostgresErrorCodes.UniqueViolation,
        } postgresException
        && postgresException.ConstraintName
            == Persistence.Configurations.CertificateConfiguration.UniqueStudentCourseIndex;

    private static string Truncate(string value) =>
        value.Length <= MaxLastErrorLength ? value : value[..MaxLastErrorLength];
}
