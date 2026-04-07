using Microsoft.AspNetCore.Mvc;
using NotificationService.Contracts.Events;
using NotificationService.Services.Notifications;
using NotificationService.Services.RabbitMq;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/test-notifications")]
public class TestNotificationsController(INotificationCreationService notificationCreationService,
    IRabbitMqPublisher rabbitMqPublisher, IConfiguration configuration) : ControllerBase
{
    [HttpPost("password-reset")]
    public async Task<IActionResult> SendPasswordResetAsync(
        [FromBody] PasswordResetRequestedEvent request,
        CancellationToken cancellationToken)
    {
        var notificationId = await notificationCreationService.CreatePasswordResetNotificationAsync(
            request,
            cancellationToken);

        return Ok(new
        {
            message = "Pending notification created.",
            notificationId
        });
    }
    
    [HttpPost("password-reset/publish")]
    public async Task<IActionResult> PublishPasswordResetAsync([FromBody] PasswordResetRequestedEvent request,
        CancellationToken cancellationToken)
    {
        var routingKey = configuration["RabbitMQ:RoutingKey"]
                         ?? throw new InvalidOperationException("RabbitMQ:RoutingKey is not configured");
        await rabbitMqPublisher.PublishAsync(request, routingKey, cancellationToken);

        return Ok(new { message = "Password reset event published to RabbitMQ." });
    }
}