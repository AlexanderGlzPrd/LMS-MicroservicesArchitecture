using Enrollments.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Enrollments.Infrastructure.Persistence.Configurations;
internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    internal const string UniqueAggregateMessageTypeIndex =
        "ux_outbox_messages_aggregate_id_message_type";

    internal const string PendingIndex = "ix_outbox_messages_pending";

    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(message => message.Id)
            .HasName("pk_outbox_messages");

        builder.Property(message => message.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(message => message.AggregateId)
            .HasColumnName("aggregate_id")
            .IsRequired();

        builder.Property(message => message.MessageType)
            .HasColumnName("message_type")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(message => message.RoutingKey)
            .HasColumnName("routing_key")
            .HasMaxLength(60);

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

        builder.HasIndex(message => new { message.AggregateId, message.MessageType })
            .HasDatabaseName(UniqueAggregateMessageTypeIndex)
            .IsUnique();

        builder.HasIndex(message => message.Id)
            .HasDatabaseName(PendingIndex)
            .HasFilter("published_at IS NULL");
    }
}