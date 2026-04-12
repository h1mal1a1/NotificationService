using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NotificationService.Configuration;
using NotificationService.Data;
using NotificationService.Entities;
using NotificationService.Services.Metrics;

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
        var pollInterval = TimeSpan.FromSeconds(Math.Max(1, _retrySettings.PollIntervalSeconds));
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var deliveryService = scope.ServiceProvider.GetRequiredService<INotificationDeliveryService>();

                var metrics = scope.ServiceProvider.GetRequiredService<NotificationMetrics>();
                var pendingCount =await dbContext.Notifications.CountAsync(
                    x => x.Status == NotificationStatus.Pending,
                        stoppingToken);
                var processingCount =await dbContext.Notifications.CountAsync(
                    x => x.Status == NotificationStatus.Processing,
                    stoppingToken);
                var sentCount =await dbContext.Notifications.CountAsync(
                    x => x.Status == NotificationStatus.Sent,
                    stoppingToken);
                var exhaustedCount =await dbContext.Notifications.CountAsync(
                    x => x.Status == NotificationStatus.Exhausted,
                    stoppingToken);
                metrics.SetStatusCount("pending",pendingCount);
                metrics.SetStatusCount("processing",processingCount);
                metrics.SetStatusCount("sent",sentCount);
                metrics.SetStatusCount("exhausted",exhaustedCount);
                
                var now = DateTime.UtcNow;
                await ReturnStuckProcessingNotificationsAsync(dbContext, now, stoppingToken);

                var batchSize = Math.Max(1, _retrySettings.BatchSize);
                var maxAttempts = Math.Max(1, _retrySettings.MaxAttempts);
                
                var pendingNotificationIds = await dbContext.Notifications
                    .Where(x => x.Status == NotificationStatus.Pending)
                    .Where(x=>x.NextAttemptAtUtc <= now)
                    .Where(x => x.RetryCount < maxAttempts)
                    .OrderBy(x => x.NextAttemptAtUtc)
                    .ThenBy(x=>x.CreatedAtUtc)
                    .Select(x => x.Id)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken: stoppingToken);
                foreach (var notificationId in pendingNotificationIds)
                {
                    if (stoppingToken.IsCancellationRequested)
                        break;
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
                await Task.Delay(pollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("Pending notifications worker stopped");
    }

    private async Task ReturnStuckProcessingNotificationsAsync(
        AppDbContext dbContext, 
        DateTime now, 
        CancellationToken cancellationToken)
    {
        var processingTimeoutSeconds = Math.Max(1, _retrySettings.ProcessingTimeoutSeconds);
        var timeoutBoundary = now.AddSeconds(-processingTimeoutSeconds);
        var stuckNotifications = await dbContext.Notifications
            .Where(x => x.Status == NotificationStatus.Processing)
            .Where(x => x.ProcessingStartedAtUtc != null)
            .Where(x => x.ProcessingStartedAtUtc < timeoutBoundary)
            .ToListAsync(cancellationToken);
        if (stuckNotifications.Count == 0) return;
        foreach (var notification in stuckNotifications)
        {
            notification.Status = NotificationStatus.Pending;
            notification.ProcessingStartedAtUtc = null;
            notification.NextAttemptAtUtc = now;
            notification.LastError = "Processing timeout exceeded. Notification returned to pending state.";
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogWarning("Returned {Count} stuck processing notifications back to pending state",
            stuckNotifications.Count);
    }
}