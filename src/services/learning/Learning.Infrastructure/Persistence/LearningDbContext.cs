using Learning.Domain.Progress;
using Learning.Infrastructure.Messaging;
using Learning.Infrastructure.Projection;
using Microsoft.EntityFrameworkCore;
namespace Learning.Infrastructure.Persistence;

public sealed class LearningDbContext(DbContextOptions<LearningDbContext> options)
    : DbContext(options)
{
    public DbSet<CourseProgress> CourseProgresses => Set<CourseProgress>();

    internal DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    internal DbSet<ProgressEvent> ProgressEvents => Set<ProgressEvent>();

    internal DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LearningDbContext).Assembly);
    }
}
