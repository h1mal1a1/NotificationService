using Microsoft.AspNetCore.Mvc;
using NotificationService.Contracts.Events;
using NotificationService.Services.Notifications;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/test-notifications")]
public class TestNotificationsController(INotificationProcessor notificationProcessor) : ControllerBase
{
    private readonly INotificationProcessor _notificationProcessor = notificationProcessor;

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
}