using Enrollments.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Enrollments.Infrastructure.Persistence.Configurations;
internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    internal const string PrimaryKeyName = "pk_inbox_messages";

    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");

        builder.HasKey(message => message.MessageId)
            .HasName(PrimaryKeyName);

        builder.Property(message => message.MessageId)
            .HasColumnName("message_id")
            .ValueGeneratedNever();

        builder.Property(message => message.MessageType)
            .HasColumnName("message_type")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(message => message.ProcessedAt)
            .HasColumnName("processed_at")
            .IsRequired();
    }
}
