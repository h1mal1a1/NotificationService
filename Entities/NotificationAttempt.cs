namespace NotificationService.Entities;

public class NotificationAttempt
{
    public long Id { get; set; }
    public long IdNotification { get; set; }
    public Notification Notification { get; set; } = null!;
    public int AttemptNumber { get; set; }
    public DateTime AttemptedAtUtc { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}