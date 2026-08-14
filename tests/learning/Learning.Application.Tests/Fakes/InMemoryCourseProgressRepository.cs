using Learning.Application.Abstractions;
using Learning.Domain.Progress;
namespace Learning.Application.Tests.Fakes;

internal sealed class InMemoryCourseProgressRepository : ICourseProgressRepository
{
    private readonly List<CourseProgress> stored = [];
    private readonly List<CourseProgress> pending = [];

    public int StoredCount => stored.Count;

    public int AddCount { get; private set; }

    public void Seed(CourseProgress progress) => stored.Add(progress);

    public void Commit()
    {
        stored.AddRange(pending);
        pending.Clear();
    }

    public void DiscardPending() => pending.Clear();

    public Task<CourseProgress?> FindAsync(
        StudentId studentId,
        CourseId courseId,
        CancellationToken cancellationToken) =>
        Task.FromResult(stored.SingleOrDefault(
            progress => progress.StudentId == studentId && progress.CourseId == courseId));

    public Task<IReadOnlyList<CourseProgress>> ListByStudentAsync(
        StudentId studentId,
        CourseProgressStatus? status,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CourseProgress>>(
        [
            .. stored
                .Where(progress => progress.StudentId == studentId)
                .Where(progress => status is null || progress.Status == status)
                .OrderByDescending(progress => progress.StartedAt)
                .ThenBy(progress => progress.CourseId.Value)
        ]);

    public void Add(CourseProgress progress)
    {
        AddCount++;
        pending.Add(progress);
    }
}
