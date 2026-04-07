using Microsoft.EntityFrameworkCore;
using NotificationService.Contracts.Events;
using NotificationService.Data;
using NotificationService.Entities;

namespace NotificationService.Services.Notifications;

public class NotificationCreationService(
    AppDbContext dbContext,
    ITemplateRenderer templateRenderer,
    ILogger<NotificationCreationService> logger)
    : INotificationCreationService
{
    private const string PasswordResetTemplateCode = "password_reset";

    public async Task<long> CreatePasswordResetNotificationAsync(PasswordResetRequestedEvent notificationEvent,
        CancellationToken cancellationToken = default)
    {
        var template = await dbContext.NotificationsTemplates
            .FirstOrDefaultAsync(
                x => x.TemplateCode == PasswordResetTemplateCode && x.Channel == NotificationChannel.Email &&
                     x.IsActive, cancellationToken);
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

        var notification = new Notification()
        {
            EventType = nameof(PasswordResetRequestedEvent),
            Channel = NotificationChannel.Email,
            Recipient = notificationEvent.Email,
            Subject = subject,
            Body = body,
            Status = NotificationStatus.Pending,
            RetryCount = 0,
            LastError = null,
            CreatedAtUtc = DateTime.UtcNow,
            SentAtUtc = null
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Pending notification {NotificationId} was created for {Recipient}", notification.Id,
            notification.Recipient);
        return notification.Id;
    }
}