using Microsoft.Extensions.Options;
using NotificationService.Configuration;

namespace NotificationService.Services.Notifications;

public class ExponentialRetryDelayCalculator(IOptions<RetrySettings> retryOptions) : IRetryDelayCalculator
{
    private readonly RetrySettings _retrySettings = retryOptions.Value;

    public DateTime GetNextAttemptAtUtc(int retryCount, DateTime utcNow)
    {
        var firstDelaySeconds = Math.Max(1, _retrySettings.FirstDelaySeconds);
        var maxDelaySeconds = Math.Max(firstDelaySeconds, _retrySettings.MaxDelaySeconds);
        var multiplier = _retrySettings.BackoffMultiplier <= 1 ? 2.0 : _retrySettings.BackoffMultiplier;
        var exponent = Math.Max(0, retryCount - 1);
        var delaySeconds = firstDelaySeconds * Math.Pow(multiplier, exponent);
        delaySeconds = Math.Min(delaySeconds, maxDelaySeconds);

        return utcNow.AddSeconds(delaySeconds);
    }
}