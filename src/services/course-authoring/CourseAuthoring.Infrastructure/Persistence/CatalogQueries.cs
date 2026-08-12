using CourseAuthoring.Application.Abstractions;
using CourseAuthoring.Application.Catalog;
using CourseAuthoring.Application.Common;
using CourseAuthoring.Application.Courses;
using CourseAuthoring.Domain.Courses;
using Microsoft.EntityFrameworkCore;

namespace CourseAuthoring.Infrastructure.Persistence;

internal sealed class CatalogQueries(CourseAuthoringDbContext context) : ICatalogQueries
{
    public async Task<PagedResult<CatalogCourseSummaryView>> BrowseAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var published = context.Courses
            .AsNoTracking()
            .Where(course => course.Status == CourseStatus.Published);

        var totalCount = await published.CountAsync(cancellationToken);

        var rows = await published
            .OrderByDescending(course => course.PublishedAt)
            .ThenBy(course => course.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(course => new
            {
                course.Id,
                Title = course.PublishedTitle!,
                course.InstructorId,
                LessonCount = context.PublishedLessons.Count(lesson => lesson.CourseId == course.Id),
                PublishedAt = course.PublishedAt!.Value,
                PublishedContentUpdatedAt = course.PublishedContentUpdatedAt!.Value,
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(row => new CatalogCourseSummaryView(
                row.Id.Value,
                row.Title,
                row.InstructorId.Value,
                row.LessonCount,
                row.PublishedAt,
                row.PublishedContentUpdatedAt))
            .ToList();

        return new PagedResult<CatalogCourseSummaryView>(items, page, pageSize, totalCount);
    }

    public async Task<CatalogCourseView?> GetPublishedCourseAsync(
        CourseId courseId,
        CancellationToken cancellationToken)
    {
        var course = await context.Courses
            .AsNoTracking()
            .Where(candidate => candidate.Id == courseId && candidate.Status == CourseStatus.Published)
            .Select(candidate => new
            {
                candidate.Id,
                Title = candidate.PublishedTitle!,
                candidate.InstructorId,
                PublishedAt = candidate.PublishedAt!.Value,
                PublishedContentUpdatedAt = candidate.PublishedContentUpdatedAt!.Value,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (course is null)
        {
            return null;
        }

        var lessons = await context.PublishedLessons
            .AsNoTracking()
            .Where(lesson => lesson.CourseId == courseId)
            .OrderBy(lesson => lesson.Position)
            .ToListAsync(cancellationToken);

        return new CatalogCourseView(
            course.Id.Value,
            course.Title,
            course.InstructorId.Value,
            course.PublishedAt,
            course.PublishedContentUpdatedAt,
            [.. lessons.Select(lesson => LessonView.From(lesson))]);
    }
}
