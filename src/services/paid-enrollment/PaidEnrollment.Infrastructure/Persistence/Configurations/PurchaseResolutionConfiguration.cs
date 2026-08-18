using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Infrastructure.Persistence.Configurations;
internal sealed class PurchaseResolutionConfiguration
    : IEntityTypeConfiguration<PurchaseResolution>
{
    internal const string PrimaryKeyName = "pk_purchase_resolutions";

    internal const string PurchaseIndex = "ix_purchase_resolutions_purchase_id";

    public void Configure(EntityTypeBuilder<PurchaseResolution> builder)
    {
        builder.ToTable("purchase_resolutions");

        builder.HasKey(resolution => resolution.Id)
            .HasName(PrimaryKeyName);

        builder.Property(resolution => resolution.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(resolution => resolution.PurchaseId)
            .HasColumnName("purchase_id")
            .HasConversion(id => id.Value, value => new PurchaseId(value))
            .IsRequired();

        builder.Property(resolution => resolution.Resolution)
            .HasColumnName("resolution")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(resolution => resolution.Evidence)
            .HasColumnName("evidence")
            .HasMaxLength(PurchaseResolution.MaxEvidenceLength)
            .IsRequired();

        builder.Property(resolution => resolution.OperatorId)
            .HasColumnName("operator_id")
            .IsRequired();

        builder.Property(resolution => resolution.ResolvedAt)
            .HasColumnName("resolved_at")
            .IsRequired();

        builder.HasIndex(resolution => resolution.PurchaseId)
            .HasDatabaseName(PurchaseIndex);
    }
}
