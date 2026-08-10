using CourseAuthoring.Domain.Courses;
using Microsoft.EntityFrameworkCore;

namespace CourseAuthoring.Infrastructure.Persistence;

public sealed class CourseAuthoringDbContext(DbContextOptions<CourseAuthoringDbContext> options)
    : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CourseAuthoringDbContext).Assembly);
    }
}
