using CourseAuthoring.Domain.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CourseAuthoring.Infrastructure.Persistence;

public sealed class CourseAuthoringDbContext(DbContextOptions<CourseAuthoringDbContext> options)
    : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();

    public DbSet<Lesson> Lessons => Set<Lesson>();

    public DbSet<PublishedLesson> PublishedLessons => Set<PublishedLesson>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CourseAuthoringDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ReconcilePublishedLessons();

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ReconcilePublishedLessons();

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ReconcilePublishedLessons()
    {
        var autoDetectChanges = ChangeTracker.AutoDetectChangesEnabled;
        ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            var tracked = ChangeTracker.Entries<PublishedLesson>()
                .Where(entry => entry.State is EntityState.Unchanged or EntityState.Modified)
                .ToDictionary(entry => entry.Entity.Id);

            if (tracked.Count == 0)
            {
                return;
            }

            foreach (var courseEntry in ChangeTracker.Entries<Course>())
            {
                Reconcile(courseEntry.Collection(nameof(Course.PublishedLessons)), tracked);
            }
        }
        finally
        {
            ChangeTracker.AutoDetectChangesEnabled = autoDetectChanges;
        }
    }

    private static void Reconcile(
        CollectionEntry navigation,
        IReadOnlyDictionary<LessonId, EntityEntry<PublishedLesson>> tracked)
    {
        if (navigation.CurrentValue is not ICollection<PublishedLesson> snapshot)
        {
            return;
        }

        foreach (var replacement in snapshot.ToList())
        {
            if (!tracked.TryGetValue(replacement.Id, out var existing)
                || ReferenceEquals(existing.Entity, replacement))
            {
                continue;
            }

            existing.CurrentValues.SetValues(replacement);

            snapshot.Remove(replacement);
            snapshot.Add(existing.Entity);
        }
    }
}
