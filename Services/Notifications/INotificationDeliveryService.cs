namespace NotificationService.Services.Notifications;

public interface INotificationDeliveryService
{
    Task DeliverAsync(long notificationId, CancellationToken cancellationToken = default);
}