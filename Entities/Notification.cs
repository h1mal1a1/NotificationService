namespace NotificationService.Entities;

public class Notification
{
    public long Id { get; set; }
    public string EventType { get; set; } = null!;
    public NotificationChannel Channel { get; set; }
    public string Recipient { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
    public NotificationStatus Status { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? SendAtUtc { get; set; }

    public ICollection<NotificationAttempt> Attempts { get; set; } = [];

}