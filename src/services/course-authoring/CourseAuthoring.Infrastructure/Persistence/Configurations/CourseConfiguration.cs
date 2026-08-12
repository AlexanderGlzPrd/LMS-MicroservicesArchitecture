using CourseAuthoring.Domain.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CourseAuthoring.Infrastructure.Persistence.Configurations;
internal sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("courses");

        builder.HasKey(course => course.Id);

        builder.Property(course => course.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new CourseId(value));

        builder.Property(course => course.InstructorId)
            .HasColumnName("instructor_id")
            .HasConversion(id => id.Value, value => new InstructorId(value))
            .IsRequired();

        builder.Property(course => course.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(course => course.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(course => course.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(course => course.PublishedTitle)
            .HasColumnName("published_title")
            .HasMaxLength(200);

        builder.Property(course => course.PublishedAt)
            .HasColumnName("published_at");

        builder.Property(course => course.PublishedContentUpdatedAt)
            .HasColumnName("published_content_updated_at");

        builder.HasMany<Lesson>(nameof(Course.WorkingLessons))
            .WithOne()
            .HasForeignKey(lesson => lesson.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(nameof(Course.WorkingLessons))
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany<PublishedLesson>(nameof(Course.PublishedLessons))
            .WithOne()
            .HasForeignKey(lesson => lesson.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(nameof(Course.PublishedLessons))
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(course => course.DomainEvents);
    }
}