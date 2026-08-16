using Learning.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Learning.Infrastructure.Persistence.Configurations;
internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    internal const string PrimaryKeyName = "pk_outbox_messages";

    internal const string UniqueStudentCourseMessageTypeIndex =
        "ux_outbox_messages_student_id_course_id_message_type";

    internal const string PendingIndex = "ix_outbox_messages_pending";

    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(message => message.Id)
            .HasName(PrimaryKeyName);

        builder.Property(message => message.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(message => message.StudentId)
            .HasColumnName("student_id")
            .IsRequired();

        builder.Property(message => message.CourseId)
            .HasColumnName("course_id")
            .IsRequired();

        builder.Property(message => message.MessageType)
            .HasColumnName("message_type")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(message => message.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(message => message.OccurredAt)
            .HasColumnName("occurred_at")
            .IsRequired();

        builder.Property(message => message.PublishedAt)
            .HasColumnName("published_at");

        builder.Property(message => message.AttemptCount)
            .HasColumnName("attempt_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(message => message.LastError)
            .HasColumnName("last_error");

        builder.Property(message => message.LastAttemptAt)
            .HasColumnName("last_attempt_at");

        builder.HasIndex(
                message => new { message.StudentId, message.CourseId, message.MessageType },
                UniqueStudentCourseMessageTypeIndex)
            .HasDatabaseName(UniqueStudentCourseMessageTypeIndex)
            .IsUnique();

        builder.HasIndex(message => message.Id, PendingIndex)
            .HasDatabaseName(PendingIndex)
            .HasFilter("published_at IS NULL");
    }
}
