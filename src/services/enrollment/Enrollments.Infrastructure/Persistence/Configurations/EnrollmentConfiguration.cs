using Enrollments.Domain.Enrollments;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enrollments.Infrastructure.Persistence.Configurations;
internal sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    internal const string UniqueStudentCourseIndex = "ix_enrollments_student_id_course_id";

    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("enrollments");

        builder.HasKey(enrollment => enrollment.Id);

        builder.Property(enrollment => enrollment.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new EnrollmentId(value));

        builder.Property(enrollment => enrollment.StudentId)
            .HasColumnName("student_id")
            .HasConversion(id => id.Value, value => new StudentId(value))
            .IsRequired();

        builder.Property(enrollment => enrollment.CourseId)
            .HasColumnName("course_id")
            .HasConversion(id => id.Value, value => new CourseId(value))
            .IsRequired();

        builder.Property(enrollment => enrollment.Type)
            .HasColumnName("enrollment_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(enrollment => enrollment.EnrolledAt)
            .HasColumnName("enrolled_at")
            .IsRequired();

        builder.HasIndex(enrollment => new { enrollment.StudentId, enrollment.CourseId })
            .HasDatabaseName(UniqueStudentCourseIndex)
            .IsUnique();
    }
}
