using NotificationService.Contracts.Events;
using NotificationService.Services.Notifications;

namespace NotificationService.Services.RabbitMq;

public class RabbitMqMessageHandler(
    INotificationCreationService notificationCreationService,
    ILogger<RabbitMqMessageHandler> logger)
{
    public async Task HandlePasswordResetAsync(PasswordResetRequestedEvent message, CancellationToken cancellationToken = default)
    {
        var notificationId =
            await notificationCreationService.CreatePasswordResetNotificationAsync(message, cancellationToken);
        logger.LogInformation("Notification event stored as pending. NotificationId: {NotificationId}", notificationId);
    }
}