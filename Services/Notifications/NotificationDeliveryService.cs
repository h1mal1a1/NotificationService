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
    IRetryDelayCalculator retryDelayCalculator,
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
            case NotificationStatus.Exhausted:
                logger.LogInformation("Notification {NotificationId} is already exhausted", notification.Id);
                return;
        }

        if (notification.Status != NotificationStatus.Pending)
        {
            logger.LogInformation("Notification {NotificationId} has status {Status} and will be skipped.",
                notification.Id, notification.Status);
            return;
        }

        var now = DateTime.UtcNow;
        var maxAttempts = Math.Max(1, _retrySettings.MaxAttempts);
        var attemptNumber = notification.RetryCount + 1;

        if (attemptNumber > maxAttempts)
        {
            notification.Status = NotificationStatus.Exhausted;
            notification.LastError ??= "Maximum retry attempts exceeded.";
            notification.ProcessingStartedAtUtc = null;
            
            await dbContext.SaveChangesAsync(cancellationToken);
            
            logger.LogWarning("Notification {NotificationId} exceeded maximum attempts and was marked as exhausted",
                notificationId);
            return;
        }

        try
        {
            notification.Status = NotificationStatus.Processing;
            notification.ProcessingStartedAtUtc = now;
            notification.LastAttemptAtUtc = now;
            
            await dbContext.SaveChangesAsync(cancellationToken);
            
            await emailSender.SendAsync(notification.Recipient, notification.Subject, notification.Body,
                cancellationToken);
            
            var sentAtUtc = DateTime.UtcNow;
            
            notification.Status = NotificationStatus.Sent;
            notification.SentAtUtc = sentAtUtc;
            notification.LastError = null;
            notification.ProcessingStartedAtUtc = null;

            dbContext.NotificationsAttempts.Add(new NotificationAttempt
            {
                IdNotification = notification.Id,
                AttemptNumber = attemptNumber,
                AttemptedAtUtc = sentAtUtc,
                IsSuccess = true,
                ErrorMessage = null
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Notification {NotificationId} was sent successfully on attempt {AttemptNumber}",
                notification.Id, attemptNumber);
        }
        catch (Exception ex)
        {
            var failedAtUtc = DateTime.UtcNow;
            
            notification.RetryCount += 1;
            notification.LastError = ex.Message;
            notification.ProcessingStartedAtUtc = null;
            
            if (notification.RetryCount >= maxAttempts)
            {
                notification.Status = NotificationStatus.Exhausted;
            }
            else
            {
                notification.Status = NotificationStatus.Pending;
                notification.NextAttemptAtUtc =
                    retryDelayCalculator.GetNextAttemptAtUtc(notification.RetryCount, failedAtUtc);
            }

            dbContext.NotificationsAttempts.Add(new NotificationAttempt
            {
                IdNotification = notification.Id,
                AttemptNumber = attemptNumber,
                AttemptedAtUtc = failedAtUtc,
                IsSuccess = false,
                ErrorMessage = ex.Message
            });

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogWarning(ex,
                "Failed to send notification {NotificationId} on attempt {Attempt}/{MaxAttempts}.",
                notification.Id, attemptNumber, maxAttempts);
        }
    }
}