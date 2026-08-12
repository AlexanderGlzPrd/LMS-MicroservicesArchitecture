using CourseAuthoring.Application.Abstractions;
using CourseAuthoring.Domain.Courses;
using Microsoft.EntityFrameworkCore;

namespace CourseAuthoring.Infrastructure.Persistence;

internal sealed class CourseRepository(CourseAuthoringDbContext context) : ICourseRepository
{
    public Task<Course?> GetByIdAsync(CourseId id, CancellationToken cancellationToken) =>
        context.Courses
            .Include(course => course.WorkingLessons.OrderBy(lesson => lesson.Position))
            .Include(course => course.PublishedLessons.OrderBy(lesson => lesson.Position))
            .FirstOrDefaultAsync(course => course.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Course>> ListByInstructorAsync(
        InstructorId instructorId,
        CancellationToken cancellationToken) =>
        await context.Courses
            .AsNoTracking()
            .Where(course => course.InstructorId == instructorId)
            .OrderByDescending(course => course.CreatedAt)
            .ThenBy(course => course.Id)
            .ToListAsync(cancellationToken);

    public void Add(Course course) => context.Courses.Add(course);
}
