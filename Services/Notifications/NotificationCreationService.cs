using Microsoft.EntityFrameworkCore;
using NotificationService.Contracts.Events;
using NotificationService.Data;
using NotificationService.Entities;
using NotificationService.Services.Metrics;

namespace NotificationService.Services.Notifications;

public class NotificationCreationService(
    AppDbContext dbContext,
    ITemplateRenderer templateRenderer,
    NotificationMetrics metrics,
    ILogger<NotificationCreationService> logger)
    : INotificationCreationService
{
    private const string PasswordResetTemplateCode = "password_reset";

    public async Task<long> CreatePasswordResetNotificationAsync(PasswordResetRequestedEvent notificationEvent,
        CancellationToken cancellationToken = default)
    {
        if (notificationEvent.MessageId == Guid.Empty)
            throw new InvalidOperationException("MessageId must not be empty");
        var existingNotification = await dbContext.Notifications
            .FirstOrDefaultAsync(x => x.MessageId == notificationEvent.MessageId, cancellationToken);
        if (existingNotification is not null)
        {
            logger.LogInformation("Notification with MessageId {MessageId} already exists. NotificationId: {NotificationId}",
                notificationEvent.MessageId,
                existingNotification.Id);

            return existingNotification.Id;
        }
        
        
        var template = await dbContext.NotificationsTemplates
            .FirstOrDefaultAsync(
                x => x.TemplateCode == PasswordResetTemplateCode 
                     && x.Channel == NotificationChannel.Email 
                     && x.IsActive, 
                cancellationToken);
        if (template is null)
            throw new InvalidOperationException(
                $"Active template '{PasswordResetTemplateCode}' for Email channel was not found");

        var templateValues = new Dictionary<string, string>()
        {
            ["UserName"] = notificationEvent.UserName,
            ["ResetLink"] = notificationEvent.ResetLink,
            ["ExpirationMinutes"] = notificationEvent.ExpirationMinutes.ToString()
        };

        var subject = templateRenderer.Render(template.SubjectTemplate, templateValues);
        var body = templateRenderer.Render(template.BodyTemplate, templateValues);
        var now = DateTime.UtcNow;
        var notification = new Notification
        {
            MessageId = notificationEvent.MessageId,
            EventType = nameof(PasswordResetRequestedEvent),
            Channel = NotificationChannel.Email,
            Recipient = notificationEvent.Email,
            Subject = subject,
            Body = body,
            Status = NotificationStatus.Pending,
            RetryCount = 0,
            LastError = null,
            CreatedAtUtc = now,
            SentAtUtc = null,
            NextAttemptAtUtc = now,
            LastAttemptAtUtc = null,
            ProcessingStartedAtUtc = null
        };

        dbContext.Notifications.Add(notification);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            metrics.IncrementCreated();
        }
        catch (DbUpdateException)
        {
            var duplicateNotification = await dbContext.Notifications
                .FirstAsync(x => x.MessageId == notificationEvent.MessageId, cancellationToken);

            logger.LogWarning(
                "Duplicate notification detected by unique index. MessageId: {MessageId}, " +
                "NotificationId: {NotificationId}", notificationEvent.MessageId, duplicateNotification.Id);
            return duplicateNotification.Id;
        }

        logger.LogInformation(
            "Pending notification {NotificationId} was created for {Recipient} with MessageId: {MessageId}", 
            notification.Id, 
            notification.Recipient, 
            notification.MessageId);
        return notification.Id;
    }
}