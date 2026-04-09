using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Entities;

namespace NotificationService.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.MessageId)
            .HasColumnName("message_id")
            .IsRequired();
        builder.HasIndex(x => x.MessageId)
            .IsUnique();
        
        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(200)
            .IsRequired();
        
        builder.Property(x => x.Channel)
            .HasColumnName("channel")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Recipient)
            .HasColumnName("recipient")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Subject)
            .HasColumnName("subject")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Body)
            .HasColumnName("body")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.RetryCount)
            .HasColumnName("retry_count")
            .IsRequired();

        builder.Property(x => x.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(x => x.SentAtUtc)
            .HasColumnName("sent_at_utc");
        
        builder.Property(x => x.NextAttemptAtUtc)
            .HasColumnName("next_attempt_at_utc")
            .IsRequired();

        builder.Property(x => x.LastAttemptAtUtc)
            .HasColumnName("last_attempt_at_utc");

        builder.Property(x => x.ProcessingStartedAtUtc)
            .HasColumnName("processing_started_at_utc");

        builder.HasIndex(x => new { x.Status, x.NextAttemptAtUtc });
    }
}