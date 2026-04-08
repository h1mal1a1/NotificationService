using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NotificationService.Configuration;
using NotificationService.Data;
using NotificationService.Entities;

namespace NotificationService.Services.Notifications;

public class PendingNotificationsWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<RetrySettings> retryOptions,
    ILogger<PendingNotificationsWorker> logger) : BackgroundService
{
    private readonly RetrySettings _retrySettings = retryOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Pending notifications worker started");
        var pollingDelay = TimeSpan.FromSeconds(Math.Max(1, _retrySettings.DelaySeconds));
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var deliveryService = scope.ServiceProvider.GetRequiredService<INotificationDeliveryService>();
                var pendingNotificationIds = await dbContext.Notifications
                    .Where(x => x.Status == NotificationStatus.Pending)
                    .Where(x => x.RetryCount < _retrySettings.MaxAttempts)
                    .OrderBy(x => x.CreatedAtUtc)
                    .Select(x => x.Id)
                    .Take(10)
                    .ToListAsync(cancellationToken: stoppingToken);
                foreach (var notificationId in pendingNotificationIds.TakeWhile(_ =>
                             !stoppingToken.IsCancellationRequested))
                {
                    logger.LogInformation("Processing notification {NotificationId}", notificationId);
                    await deliveryService.DeliverAsync(notificationId, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred in pending notifications worker");
            }

            try
            {
                await Task.Delay(pollingDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("Pending notifications worker stopped");
    }
}