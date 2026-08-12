using CourseAuthoring.Domain.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CourseAuthoring.Infrastructure.Persistence.Configurations;
internal sealed class PublishedLessonConfiguration : IEntityTypeConfiguration<PublishedLesson>
{
    public void Configure(EntityTypeBuilder<PublishedLesson> builder)
    {
        builder.ToTable("published_lessons");

        builder.HasKey(lesson => lesson.Id);

        builder.Property(lesson => lesson.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new LessonId(value));

        builder.Property(lesson => lesson.CourseId)
            .HasColumnName("course_id")
            .HasConversion(id => id.Value, value => new CourseId(value))
            .IsRequired();

        builder.Property(lesson => lesson.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(lesson => lesson.Description)
            .HasColumnName("description")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(lesson => lesson.VideoUrl)
            .HasColumnName("video_url")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(lesson => lesson.Position)
            .HasColumnName("position")
            .IsRequired();

        builder.HasIndex(lesson => new { lesson.CourseId, lesson.Position })
            .HasDatabaseName("ix_published_lessons_course_id_position");
    }
}
