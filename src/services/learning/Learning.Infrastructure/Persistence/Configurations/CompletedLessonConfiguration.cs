using Learning.Domain.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Learning.Infrastructure.Persistence.Configurations;

internal sealed class CompletedLessonConfiguration : IEntityTypeConfiguration<CompletedLesson>
{
    internal const string PrimaryKeyName = "pk_completed_lessons";
    internal const string StudentIdShadowProperty = "StudentId";
    internal const string CourseIdShadowProperty = "CourseId";

    public void Configure(EntityTypeBuilder<CompletedLesson> builder)
    {
        builder.ToTable("completed_lessons");

        builder.Property<StudentId>(StudentIdShadowProperty)
            .HasColumnName("student_id")
            .HasConversion(id => id.Value, value => new StudentId(value));

        builder.Property<CourseId>(CourseIdShadowProperty)
            .HasColumnName("course_id")
            .HasConversion(id => id.Value, value => new CourseId(value));

        builder.HasKey(
                StudentIdShadowProperty,
                CourseIdShadowProperty,
                nameof(CompletedLesson.LessonId))
            .HasName(PrimaryKeyName);

        builder.Property(lesson => lesson.LessonId)
            .HasColumnName("lesson_id")
            .HasConversion(id => id.Value, value => new LessonId(value));

        builder.Property(lesson => lesson.CompletedAt)
            .HasColumnName("completed_at")
            .IsRequired();
    }
}
