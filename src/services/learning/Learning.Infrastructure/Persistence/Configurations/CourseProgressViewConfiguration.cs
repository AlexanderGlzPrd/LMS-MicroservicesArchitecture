using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Learning.Infrastructure.Persistence.Configurations;
internal sealed class CourseProgressViewConfiguration
    : IEntityTypeConfiguration<CourseProgressViewRow>
{
    internal const string TableName = "course_progress_view";

    internal const string PrimaryKeyName = "pk_course_progress_view";

    internal const string LessonArraysAlignedConstraint =
        "ck_course_progress_view_lesson_arrays_aligned";

    public void Configure(EntityTypeBuilder<CourseProgressViewRow> builder)
    {
        builder.ToTable(TableName, table => table.HasCheckConstraint(
            LessonArraysAlignedConstraint,
            "cardinality(completed_lesson_ids) = cardinality(completed_lesson_dates)"));

        builder.HasKey(row => new { row.StudentId, row.CourseId })
            .HasName(PrimaryKeyName);

        builder.Property(row => row.StudentId)
            .HasColumnName("student_id");

        builder.Property(row => row.CourseId)
            .HasColumnName("course_id");

        builder.Property(row => row.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(row => row.StartedAt)
            .HasColumnName("started_at")
            .IsRequired();

        builder.Property(row => row.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(row => row.CompletedLessonIds)
            .HasColumnName("completed_lesson_ids")
            .HasColumnType("uuid[]")
            .HasDefaultValueSql("'{}'")
            .IsRequired();

        builder.Property(row => row.CompletedLessonDates)
            .HasColumnName("completed_lesson_dates")
            .HasColumnType("timestamptz[]")
            .HasDefaultValueSql("'{}'")
            .IsRequired();

        builder.Property(row => row.CompletedLessonCount)
            .HasColumnName("completed_lesson_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(row => row.TotalLessonCount)
            .HasColumnName("total_lesson_count");
    }
}
