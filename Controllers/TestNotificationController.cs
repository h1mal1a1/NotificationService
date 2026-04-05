using Microsoft.AspNetCore.Mvc;
using NotificationService.Contracts.Events;
using NotificationService.Services.Notifications;
using NotificationService.Services.RabbitMq;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/test-notifications")]
public class TestNotificationsController(
    INotificationProcessor notificationProcessor,
    IRabbitMqPublisher rabbitMqPublisher,
    IConfiguration configuration
    ) : ControllerBase
{
    private readonly INotificationProcessor _notificationProcessor = notificationProcessor;
    private readonly IRabbitMqPublisher _rabbitMqPublisher = rabbitMqPublisher;
    private readonly IConfiguration _configuration = configuration;

    [HttpPost("password-reset")]
    public async Task<IActionResult> SendPasswordResetAsync(
        [FromBody] PasswordResetRequestedEvent request,
        CancellationToken cancellationToken)
    {
        var notificationId = await _notificationProcessor.ProcessPasswordResetAsync(
            request,
            cancellationToken);

        return Ok(new
        {
            message = "Notification processed.",
            notificationId
        });
    }
    
    [HttpPost("password-reset/publish")]
    public async Task<IActionResult> PublishPasswordResetAsync([FromBody] PasswordResetRequestedEvent request,
        CancellationToken cancellationToken)
    {
        var routingKey = _configuration["RabbitMQ:RoutingKey"]
                         ?? throw new InvalidOperationException("RabbitMq:RoutingKey is not configured");
        await _rabbitMqPublisher.PublishAsync(request, routingKey, cancellationToken);

        return Ok(new { message = "Password reset event published to RabbitMq." });
    }
}