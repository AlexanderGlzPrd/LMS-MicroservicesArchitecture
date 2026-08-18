using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Enrollments.Infrastructure.Persistence.Configurations;
internal sealed class PurchaseGrantConfiguration : IEntityTypeConfiguration<PurchaseGrant>
{
    internal const string PrimaryKeyName = "pk_purchase_grants";

    public void Configure(EntityTypeBuilder<PurchaseGrant> builder)
    {
        builder.ToTable("purchase_grants");

        builder.HasKey(grant => grant.PurchaseId)
            .HasName(PrimaryKeyName);

        builder.Property(grant => grant.PurchaseId)
            .HasColumnName("purchase_id")
            .ValueGeneratedNever();

        builder.Property(grant => grant.StudentId)
            .HasColumnName("student_id")
            .IsRequired();

        builder.Property(grant => grant.CourseId)
            .HasColumnName("course_id")
            .IsRequired();

        builder.Property(grant => grant.Outcome)
            .HasColumnName("outcome")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(grant => grant.Origin)
            .HasColumnName("origin")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(grant => grant.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasMaxLength(40);

        builder.Property(grant => grant.InitialMessageId)
            .HasColumnName("initial_message_id")
            .IsRequired();

        builder.Property(grant => grant.ProcessedAt)
            .HasColumnName("processed_at")
            .IsRequired();
    }
}