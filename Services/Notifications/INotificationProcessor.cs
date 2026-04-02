using NotificationService.Contracts.Events;

namespace NotificationService.Services.Notifications;

public interface INotificationProcessor
{
    Task<long> ProcessPasswordResetAsync(
        PasswordResetRequestedEvent notificationEvent,
        CancellationToken cancellationToken = default);
}