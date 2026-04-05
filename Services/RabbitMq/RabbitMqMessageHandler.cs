using System.Text.Json;
using NotificationService.Contracts.Events;
using NotificationService.Services.Notifications;

namespace NotificationService.Services.RabbitMq;

public class RabbitMqMessageHandler(INotificationProcessor notificationProcessor, ILogger<RabbitMqMessageHandler> logger)
{
    private readonly INotificationProcessor _notificationProcessor = notificationProcessor;
    private readonly ILogger<RabbitMqMessageHandler> _logger = logger;

    public async Task HandlePasswordResetAsync(string message, CancellationToken cancellationToken = default)
    {
        var notificationEvent = JsonSerializer.Deserialize<PasswordResetRequestedEvent>(message);
        if (notificationEvent is null)
            throw new InvalidOperationException("Failed to deserialize PasswordResetRequestedEvent.");

        await _notificationProcessor.ProcessPasswordResetAsync(notificationEvent, cancellationToken);
        _logger.LogInformation("PasswordResetRequestedEvent for {Email} processed successfully",
            notificationEvent.Email);
    }
}