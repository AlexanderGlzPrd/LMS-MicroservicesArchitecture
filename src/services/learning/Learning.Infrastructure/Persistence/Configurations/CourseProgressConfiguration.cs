using Learning.Domain.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learning.Infrastructure.Persistence.Configurations;

internal sealed class CourseProgressConfiguration : IEntityTypeConfiguration<CourseProgress>
{
    internal const string PrimaryKeyName = "pk_course_progress";
    internal const string CompletedLessonsForeignKeyName = "fk_completed_lessons_course_progress";

    public void Configure(EntityTypeBuilder<CourseProgress> builder)
    {
        builder.ToTable("course_progress");

        builder.HasKey(progress => new { progress.StudentId, progress.CourseId })
            .HasName(PrimaryKeyName);

        builder.Property(progress => progress.StudentId)
            .HasColumnName("student_id")
            .HasConversion(id => id.Value, value => new StudentId(value));

        builder.Property(progress => progress.CourseId)
            .HasColumnName("course_id")
            .HasConversion(id => id.Value, value => new CourseId(value));

        builder.Property(progress => progress.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(progress => progress.StartedAt)
            .HasColumnName("started_at")
            .IsRequired();

        builder.Property(progress => progress.CompletedAt)
            .HasColumnName("completed_at");

        builder.HasMany<CompletedLesson>(nameof(CourseProgress.CompletedLessons))
            .WithOne()
            .HasForeignKey(
                CompletedLessonConfiguration.StudentIdShadowProperty,
                CompletedLessonConfiguration.CourseIdShadowProperty)
            .HasConstraintName(CompletedLessonsForeignKeyName)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(nameof(CourseProgress.CompletedLessons))
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
