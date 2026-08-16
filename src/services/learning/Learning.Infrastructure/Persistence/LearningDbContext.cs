using Learning.Domain.Progress;
using Learning.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
namespace Learning.Infrastructure.Persistence;

public sealed class LearningDbContext(DbContextOptions<LearningDbContext> options)
    : DbContext(options)
{
    public DbSet<CourseProgress> CourseProgresses => Set<CourseProgress>();

    internal DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LearningDbContext).Assembly);
    }
}
