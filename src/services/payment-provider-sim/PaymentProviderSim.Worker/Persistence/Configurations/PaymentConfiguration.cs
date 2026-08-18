using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentProviderSim.Worker.Payments;
namespace PaymentProviderSim.Worker.Persistence.Configurations;
internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    internal const string PrimaryKeyName = "pk_payments";

    internal const string PurchaseIndex = "ix_payments_purchase_id";

    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(payment => payment.PaymentId)
            .HasName(PrimaryKeyName);

        builder.Property(payment => payment.PaymentId)
            .HasColumnName("payment_id")
            .ValueGeneratedNever();

        builder.Property(payment => payment.PurchaseId)
            .HasColumnName("purchase_id")
            .IsRequired();

        builder.Property(payment => payment.Amount)
            .HasColumnName("amount")
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(payment => payment.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(payment => payment.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(payment => payment.AuthorizedAt)
            .HasColumnName("authorized_at");

        builder.Property(payment => payment.CapturedAt)
            .HasColumnName("captured_at");

        builder.Property(payment => payment.VoidedAt)
            .HasColumnName("voided_at");

        builder.Property(payment => payment.RefundedAt)
            .HasColumnName("refunded_at");

        builder.Property(payment => payment.LastFailureReason)
            .HasColumnName("last_failure_reason")
            .HasMaxLength(40);

        builder.Property(payment => payment.SuppressedReplyCount)
            .HasColumnName("suppressed_reply_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(payment => payment.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(payment => payment.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(payment => payment.PurchaseId)
            .HasDatabaseName(PurchaseIndex);
    }
}
