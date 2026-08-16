using Learning.Application.Abstractions;
using Learning.Application.Progress;
using Learning.Domain.Progress;
using Microsoft.EntityFrameworkCore;
namespace Learning.Infrastructure.Persistence;

internal sealed class CourseProgressReadModel(LearningDbContext context) : ICourseProgressReadModel
{
    public async Task<CourseProgressView?> FindAsync(
        StudentId studentId,
        CourseId courseId,
        CancellationToken cancellationToken)
    {
        var row = await context.CourseProgressViews
            .AsNoTracking()
            .FirstOrDefaultAsync(
                view => view.StudentId == studentId.Value && view.CourseId == courseId.Value,
                cancellationToken);

        return row is null ? null : ToView(row);
    }

    public async Task<IReadOnlyList<CourseProgressView>> ListByStudentAsync(
        StudentId studentId,
        CourseProgressStatus? status,
        CancellationToken cancellationToken)
    {
        var query = context.CourseProgressViews
            .AsNoTracking()
            .Where(view => view.StudentId == studentId.Value);

        if (status is not null)
        {
            var statusName = status.ToString()!;

            query = query.Where(view => view.Status == statusName);
        }

        var rows = await query
            .OrderByDescending(view => view.StartedAt)
            .ThenBy(view => view.CourseId)
            .ToListAsync(cancellationToken);

        return [.. rows.Select(ToView)];
    }

    private static CourseProgressView ToView(CourseProgressViewRow row) => new(
        row.StudentId,
        row.CourseId,
        row.Status,
        row.StartedAt,
        row.CompletedAt,
        row.CompletedLessonIds);
}
