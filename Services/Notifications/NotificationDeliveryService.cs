using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NotificationService.Configuration;
using NotificationService.Data;
using NotificationService.Entities;
using NotificationService.Services.Email;

namespace NotificationService.Services.Notifications;

public class NotificationDeliveryService(
    AppDbContext dbContext,
    IEmailSender emailSender,
    IOptions<RetrySettings> retryOptions,
    ILogger<NotificationDeliveryService> logger) : INotificationDeliveryService
{
    private readonly RetrySettings _retrySettings = retryOptions.Value;

    public async Task DeliverAsync(long notificationId, CancellationToken cancellationToken = default)
    {
        var notification =
            await dbContext.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId, cancellationToken);

        if (notification is null)
        {
            logger.LogWarning("Notification {notificationId} was not found", notificationId);
            return;
        }

        switch (notification.Status)
        {
            case NotificationStatus.Sent:
                logger.LogInformation("Notification {NotificationId} is already sent", notification.Id);
                return;
            case NotificationStatus.Failed:
                logger.LogInformation("Notification {NotificationId} is already failed", notification.Id);
                return;
        }

        if (notification.Status != NotificationStatus.Pending)
        {
            logger.LogInformation("Notification {NotificationId} has status {Status} and will be skipped.",
                notification.Id, notification.Status);
            return;
        }
        
        var maxAttempts = Math.Max(1, _retrySettings.MaxAttempts);
        var attemptNumber = notification.RetryCount + 1;

        if (attemptNumber > maxAttempts)
        {
            notification.Status = NotificationStatus.Failed;
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Notification {NotificationId} exceeded maximum attempts and was marked as failed",
                notificationId);
            return;
        }

        try
        {
            notification.Status = NotificationStatus.Processing;
            await dbContext.SaveChangesAsync(cancellationToken);
            await emailSender.SendAsync(notification.Recipient, notification.Subject, notification.Body,
                cancellationToken);

            notification.Status = NotificationStatus.Sent;
            notification.SentAtUtc = DateTime.UtcNow;
            notification.LastError = null;
            notification.RetryCount = attemptNumber - 1;

            dbContext.NotificationsAttempts.Add(new NotificationAttempt
            {
                IdNotification = notification.Id,
                AttemptNumber = attemptNumber,
                AttemptedAtUtc = DateTime.UtcNow,
                IsSuccess = true,
                ErrorMessage = null
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Notification {NotificationId} was sent successfully on attempt {Attempt}",
                notification.Id, attemptNumber);
        }
        catch (Exception ex)
        {
            notification.RetryCount = attemptNumber;
            notification.LastError = ex.Message;
            notification.Status = attemptNumber < maxAttempts
                ? NotificationStatus.Pending
                : NotificationStatus.Failed;

            dbContext.NotificationsAttempts.Add(new NotificationAttempt()
            {
                IdNotification = notification.Id,
                AttemptNumber = attemptNumber,
                AttemptedAtUtc = DateTime.UtcNow,
                IsSuccess = false,
                ErrorMessage = ex.Message
            });

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogWarning(ex,
                "Failed to send notification {NotificationId} on attempt {Attempt}/{MaxAttempts}.",
                notification.Id, attemptNumber, maxAttempts
            );
        }
    }
}