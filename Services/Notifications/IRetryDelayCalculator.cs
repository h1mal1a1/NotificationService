namespace NotificationService.Services.Notifications;

public interface IRetryDelayCalculator
{
    DateTime GetNextAttemptAtUtc(int retryCount, DateTime utcNow);
}