using Learning.Infrastructure.Projection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Learning.Infrastructure.Persistence.Configurations;
internal sealed class ProgressEventConfiguration : IEntityTypeConfiguration<ProgressEvent>
{
    internal const string PrimaryKeyName = "pk_progress_events";

    internal const string UniqueSequenceNoIndex = "ux_progress_events_sequence_no";

    internal const string PendingIndex = "ix_progress_events_pending";

    public void Configure(EntityTypeBuilder<ProgressEvent> builder)
    {
        builder.ToTable("progress_events");

        builder.HasKey(progressEvent => progressEvent.Id)
            .HasName(PrimaryKeyName);

        builder.Property(progressEvent => progressEvent.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(progressEvent => progressEvent.SequenceNo)
            .HasColumnName("sequence_no")
            .UseIdentityAlwaysColumn()
            .ValueGeneratedOnAdd();

        builder.Property(progressEvent => progressEvent.StudentId)
            .HasColumnName("student_id")
            .IsRequired();

        builder.Property(progressEvent => progressEvent.CourseId)
            .HasColumnName("course_id")
            .IsRequired();

        builder.Property(progressEvent => progressEvent.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(progressEvent => progressEvent.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(progressEvent => progressEvent.OccurredAt)
            .HasColumnName("occurred_at")
            .IsRequired();

        builder.Property(progressEvent => progressEvent.AppliedAt)
            .HasColumnName("applied_at");

        builder.Property(progressEvent => progressEvent.AttemptCount)
            .HasColumnName("attempt_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(progressEvent => progressEvent.LastError)
            .HasColumnName("last_error");

        builder.Property(progressEvent => progressEvent.LastAttemptAt)
            .HasColumnName("last_attempt_at");

        builder.HasIndex(progressEvent => progressEvent.SequenceNo, UniqueSequenceNoIndex)
            .HasDatabaseName(UniqueSequenceNoIndex)
            .IsUnique();

        builder.HasIndex(progressEvent => progressEvent.SequenceNo, PendingIndex)
            .HasDatabaseName(PendingIndex)
            .HasFilter("applied_at IS NULL");
    }
}
