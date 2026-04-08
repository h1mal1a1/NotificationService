namespace NotificationService.Contracts.Events;

public class PasswordResetRequestedEvent
{
    public Guid MessageId { get; set; }
    public string Email { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string ResetLink { get; set; } = null!;
    public int ExpirationMinutes { get; set; }
}