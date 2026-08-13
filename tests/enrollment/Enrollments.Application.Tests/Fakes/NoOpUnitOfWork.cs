using Enrollments.Application.Abstractions;

namespace Enrollments.Application.Tests.Fakes;

internal sealed class NoOpUnitOfWork(InMemoryEnrollmentRepository repository) : IUnitOfWork
{
    public int SaveChangesCount { get; private set; }

    public Action? BeforeSave { get; set; }

    public Exception? ThrowOnSave { get; set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCount++;

        BeforeSave?.Invoke();

        if (ThrowOnSave is not null)
        {
            repository.DiscardPending();

            throw ThrowOnSave;
        }

        repository.Commit();

        return Task.CompletedTask;
    }
}
