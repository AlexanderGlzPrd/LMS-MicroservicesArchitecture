using CourseAuthoring.Application.Abstractions;
using CourseAuthoring.Domain.Courses;
using Microsoft.EntityFrameworkCore;

namespace CourseAuthoring.Infrastructure.Persistence;

internal sealed class CourseRepository(CourseAuthoringDbContext context) : ICourseRepository
{
    public Task<Course?> GetByIdAsync(CourseId id, CancellationToken cancellationToken) =>
        context.Courses.FirstOrDefaultAsync(course => course.Id == id, cancellationToken);

    public void Add(Course course) => context.Courses.Add(course);
}
