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

        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(200)
            .IsRequired();
        
        builder.Property(x => x.Channel)
            .HasColumnName("channel")
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
    }
}