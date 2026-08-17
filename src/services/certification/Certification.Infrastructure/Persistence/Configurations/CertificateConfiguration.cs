using Certification.Domain.Certificates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Certification.Infrastructure.Persistence.Configurations;
internal sealed class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    internal const string PrimaryKeyName = "pk_certificates";

    internal const string UniqueStudentCourseIndex = "ux_certificates_student_id_course_id";

    internal const string StudentIndex = "ix_certificates_student_id";

    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.ToTable("certificates");

        builder.HasKey(certificate => certificate.CertificateId)
            .HasName(PrimaryKeyName);

        builder.Property(certificate => certificate.CertificateId)
            .HasColumnName("certificate_id")
            .HasConversion(id => id.Value, value => new CertificateId(value))
            .ValueGeneratedNever();

        builder.Property(certificate => certificate.StudentId)
            .HasColumnName("student_id")
            .IsRequired();

        builder.Property(certificate => certificate.CourseId)
            .HasColumnName("course_id")
            .IsRequired();

        builder.Property(certificate => certificate.StudentName)
            .HasColumnName("student_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(certificate => certificate.CourseTitle)
            .HasColumnName("course_title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(certificate => certificate.CompletedAt)
            .HasColumnName("completed_at")
            .IsRequired();

        builder.Property(certificate => certificate.IssuedAt)
            .HasColumnName("issued_at")
            .IsRequired();

        builder.Property(certificate => certificate.Issuer)
            .HasColumnName("issuer")
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(certificate => new { certificate.StudentId, certificate.CourseId })
            .HasDatabaseName(UniqueStudentCourseIndex)
            .IsUnique();

        builder.HasIndex(certificate => certificate.StudentId)
            .HasDatabaseName(StudentIndex);

        builder.Ignore(certificate => certificate.DomainEvents);
    }
}
