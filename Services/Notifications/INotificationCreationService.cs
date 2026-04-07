using NotificationService.Contracts.Events;

namespace NotificationService.Services.Notifications;

public interface INotificationCreationService
{
    Task<long> CreatePasswordResetNotificationAsync(PasswordResetRequestedEvent message,
        CancellationToken cancellationToken = default);
}