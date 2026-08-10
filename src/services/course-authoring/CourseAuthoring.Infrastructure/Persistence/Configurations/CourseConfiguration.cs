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
    }
}