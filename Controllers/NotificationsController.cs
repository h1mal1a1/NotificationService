using Microsoft.AspNetCore.Mvc;
using NotificationService.Contracts.Events;
using NotificationService.Services.Notifications;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController(INotificationCreationService notificationCreationService) : ControllerBase
{
    [HttpPost("password-reset")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePasswordResetNotificationAsync(
        [FromBody] PasswordResetRequestedEvent request, CancellationToken cancellationToken)
    {
        var notificationId =
            await notificationCreationService.CreatePasswordResetNotificationAsync(request, cancellationToken);
        return Accepted(new { message = "Password reset notification accepted.", notificationId });
    }
}