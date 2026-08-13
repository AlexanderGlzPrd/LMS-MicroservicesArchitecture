using Learning.Application.Abstractions;
using Learning.Domain.Progress;
using Microsoft.EntityFrameworkCore;
namespace Learning.Infrastructure.Persistence;

internal sealed class CourseProgressRepository(LearningDbContext context) : ICourseProgressRepository
{
    public Task<CourseProgress?> FindAsync(
        StudentId studentId,
        CourseId courseId,
        CancellationToken cancellationToken) =>
        context.CourseProgresses
            .Include(nameof(CourseProgress.CompletedLessons))
            .FirstOrDefaultAsync(
                progress => progress.StudentId == studentId && progress.CourseId == courseId,
                cancellationToken);

    public async Task<IReadOnlyList<CourseProgress>> ListByStudentAsync(
        StudentId studentId,
        CourseProgressStatus? status,
        CancellationToken cancellationToken)
    {
        var query = context.CourseProgresses
            .AsNoTracking()
            .Include(nameof(CourseProgress.CompletedLessons))
            .Where(progress => progress.StudentId == studentId);

        if (status is not null)
        {
            query = query.Where(progress => progress.Status == status);
        }

        return await query
            .OrderByDescending(progress => progress.StartedAt)
            .ThenBy(progress => progress.CourseId)
            .ToListAsync(cancellationToken);
    }

    public void Add(CourseProgress progress) => context.CourseProgresses.Add(progress);
}
