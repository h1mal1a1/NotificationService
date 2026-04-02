using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Entities;

namespace NotificationService.Data.Configurations;

public class NotificationAttemptConfiguration : IEntityTypeConfiguration<NotificationAttempt>
{
    public void Configure(EntityTypeBuilder<NotificationAttempt> builder)
    {
        builder.ToTable("notification_attempts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id");
        builder.Property(x => x.IdNotification)
            .HasColumnName("id_notification")
            .IsRequired();
        
        builder.Property(x => x.AttemptNumber)
            .HasColumnName("attempt_number")
            .IsRequired();

        builder.Property(x => x.AttemptedAtUtc)
            .HasColumnName("attempted_at_utc")
            .IsRequired();

        builder.Property(x => x.IsSuccess)
            .HasColumnName("is_success")
            .IsRequired();

        builder.Property(x => x.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(2000);

        builder.HasOne(x => x.Notification)
            .WithMany(x => x.Attempts)
            .HasForeignKey(x => x.IdNotification)
            .OnDelete(DeleteBehavior.Cascade);
    }
}