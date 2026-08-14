using Learning.Application.Abstractions;
namespace Learning.Application.Tests.Fakes;

internal sealed class NoOpUnitOfWork(InMemoryCourseProgressRepository repository) : IUnitOfWork
{
    public int SaveChangesCount { get; private set; }
    public Exception? ThrowOnSave { get; set; }

    public int ThrowOnCall { get; set; } = 1;

    public Action? BeforeThrow { get; set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCount++;

        if (ThrowOnSave is not null && SaveChangesCount == ThrowOnCall)
        {
            repository.DiscardPending();
            BeforeThrow?.Invoke();

            throw ThrowOnSave;
        }

        repository.Commit();

        return Task.CompletedTask;
    }
}
