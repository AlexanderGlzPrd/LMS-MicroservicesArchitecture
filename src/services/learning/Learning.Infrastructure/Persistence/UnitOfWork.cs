using Learning.Application.Abstractions;
using Learning.Application.Abstractions.Exceptions;
using Learning.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Learning.Infrastructure.Persistence;

internal sealed class UnitOfWork(LearningDbContext context) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsKnownRace(exception))
        {
            context.ChangeTracker.Clear();
            throw new ConcurrentCourseProgressException(exception);
        }
    }

    private static bool IsKnownRace(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: CourseProgressConfiguration.PrimaryKeyName
                or CompletedLessonConfiguration.PrimaryKeyName,
        };
}
