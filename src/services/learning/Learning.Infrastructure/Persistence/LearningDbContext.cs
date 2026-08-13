using Learning.Domain.Progress;
using Microsoft.EntityFrameworkCore;
namespace Learning.Infrastructure.Persistence;

public sealed class LearningDbContext(DbContextOptions<LearningDbContext> options)
    : DbContext(options)
{
    public DbSet<CourseProgress> CourseProgresses => Set<CourseProgress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LearningDbContext).Assembly);
    }
}
