using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Infrastructure.Persistence.Configurations;
internal sealed class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    internal const string PrimaryKeyName = "pk_purchases";
    internal const string ActiveStudentCourseIndex = "ux_purchases_student_course_active";

    internal const string PaymentIndex = "ux_purchases_payment_id";

    internal const string ActiveFilter =
        "status NOT IN ('Confirmed', 'Rejected', 'Compensated')";

    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("purchases");

        builder.HasKey(purchase => purchase.Id)
            .HasName(PrimaryKeyName);

        builder.Property(purchase => purchase.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new PurchaseId(value))
            .ValueGeneratedNever();

        builder.Property(purchase => purchase.StudentId)
            .HasColumnName("student_id")
            .HasConversion(id => id.Value, value => new StudentId(value))
            .IsRequired();

        builder.Property(purchase => purchase.CourseId)
            .HasColumnName("course_id")
            .HasConversion(id => id.Value, value => new CourseId(value))
            .IsRequired();

        builder.Property(purchase => purchase.PaymentId)
            .HasColumnName("payment_id")
            .HasConversion(id => id.Value, value => new PaymentId(value))
            .IsRequired();

        builder.ComplexProperty(purchase => purchase.Price, price =>
        {
            price.Property(money => money.Amount)
                .HasColumnName("amount")
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            price.Property(money => money.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(purchase => purchase.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(purchase => purchase.Reason)
            .HasColumnName("reason")
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(purchase => purchase.AuthorizedAt)
            .HasColumnName("authorized_at");

        builder.Property(purchase => purchase.CapturedAt)
            .HasColumnName("captured_at");

        builder.Property(purchase => purchase.VoidedAt)
            .HasColumnName("voided_at");

        builder.Property(purchase => purchase.RefundedAt)
            .HasColumnName("refunded_at");

        builder.Property(purchase => purchase.GrantOutcome)
            .HasColumnName("grant_outcome")
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(purchase => purchase.StepStartedAt)
            .HasColumnName("step_started_at")
            .IsRequired();

        builder.Property(purchase => purchase.StepAttempts)
            .HasColumnName("step_attempts")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(purchase => purchase.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(purchase => purchase.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(purchase => new { purchase.StudentId, purchase.CourseId })
            .HasDatabaseName(ActiveStudentCourseIndex)
            .HasFilter(ActiveFilter)
            .IsUnique();

        builder.HasIndex(purchase => purchase.PaymentId)
            .HasDatabaseName(PaymentIndex)
            .IsUnique();
    }
}
