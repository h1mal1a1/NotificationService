namespace NotificationService.Configuration;

public class RetrySettings
{
    public const string SectionName = "Retry";
    public int MaxAttempts { get; set; }
    public int FirstDelaySeconds { get; set; }
    public int MaxDelaySeconds { get; set; }
    public double BackoffMultiplier { get; set; }
    public int PollIntervalSeconds { get; set; }
    public int BatchSize { get; set; }
    public int ProcessingTimeoutSeconds { get; set; }
    
}