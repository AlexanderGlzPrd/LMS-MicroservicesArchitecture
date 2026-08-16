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

    public void Add(CourseProgress progress) => context.CourseProgresses.Add(progress);
}
