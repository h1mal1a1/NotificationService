namespace NotificationService.Configuration;

public class RetrySettings
{
    public const string SectionName = "Retry";
    public int MaxAttempts { get; set; }
    public int DelaySeconds { get; set; }
}