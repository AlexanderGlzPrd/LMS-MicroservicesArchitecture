using CourseAuthoring.Application.Abstractions;

namespace CourseAuthoring.Application.Tests.Fakes;

internal sealed class NoOpUnitOfWork : IUnitOfWork
{
    public int SaveChangesCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCount++;

        return Task.CompletedTask;
    }
}
