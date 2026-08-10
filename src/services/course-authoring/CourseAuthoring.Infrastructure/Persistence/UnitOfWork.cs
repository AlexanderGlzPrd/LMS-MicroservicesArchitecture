using CourseAuthoring.Application.Abstractions;

namespace CourseAuthoring.Infrastructure.Persistence;

internal sealed class UnitOfWork(CourseAuthoringDbContext context) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
