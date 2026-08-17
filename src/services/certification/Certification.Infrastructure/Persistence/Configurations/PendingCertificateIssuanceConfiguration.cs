using Certification.Infrastructure.Issuance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Certification.Infrastructure.Persistence.Configurations;
internal sealed class PendingCertificateIssuanceConfiguration
    : IEntityTypeConfiguration<PendingCertificateIssuance>
{
    internal const string PrimaryKeyName = "pk_pending_certificate_issuances";

    public void Configure(EntityTypeBuilder<PendingCertificateIssuance> builder)
    {
        builder.ToTable("pending_certificate_issuances");

        // Una pendiente por Finalizacion.
        builder.HasKey(pending => new { pending.StudentId, pending.CourseId })
            .HasName(PrimaryKeyName);

        builder.Property(pending => pending.StudentId)
            .HasColumnName("student_id");

        builder.Property(pending => pending.CourseId)
            .HasColumnName("course_id");

        builder.Property(pending => pending.CompletedAt)
            .HasColumnName("completed_at")
            .IsRequired();

        builder.Property(pending => pending.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(pending => pending.AttemptCount)
            .HasColumnName("attempt_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(pending => pending.LastError)
            .HasColumnName("last_error");

        builder.Property(pending => pending.LastAttemptAt)
            .HasColumnName("last_attempt_at");
    }
}
