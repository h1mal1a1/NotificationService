using Prometheus;

namespace NotificationService.Services.Metrics;

public class NotificationMetrics
{
    private readonly Counter _notificationsCreated = Prometheus.Metrics.CreateCounter(
        "notification_created_total", 
        "Total number of notifications created.");
    private readonly Counter _notificationsSent = Prometheus.Metrics.CreateCounter(
        "notification_sent_total", 
        "Total number of notifications successfully sent.");
    private readonly Counter _failedAttempts = Prometheus.Metrics.CreateCounter(
        "notification_failed_attempt_total", 
        "Total number of failed notification delivery attempts.");
    private readonly Counter _notificationsExhausted = Prometheus.Metrics.CreateCounter(
        "notification_exhausted_total", 
        "Total number of notifications moved to exhausted status.");
    private readonly Counter _notificationAttempts = Prometheus.Metrics.CreateCounter(
        "notification_attempt_total",
        "Total number of notification delivery attempts.",
        new CounterConfiguration { LabelNames = ["result"] });

    private readonly Gauge _notificationsByStatus = Prometheus.Metrics.CreateGauge(
        "notification_status_total",
        "Current number of notifications by status",
        new GaugeConfiguration { LabelNames = ["status"] });

    private readonly Histogram _deliveryDuration = Prometheus.Metrics.CreateHistogram(
        "notification_delivery_duration_seconds",
        "Time spent sending notification",
        new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(0.01, 2, 10) });

    private readonly Histogram _retryCountHistogram = Prometheus.Metrics.CreateHistogram(
        "notification_retry_count",
        "Number of retries per notification",
        new HistogramConfiguration { Buckets = [0, 1, 2, 3, 5, 10] });
    public void IncrementCreated() => _notificationsCreated.Inc();

    public void IncrementSent()
    {
        _notificationsSent.Inc();
        _notificationAttempts.WithLabels("success").Inc();
    }

    public void IncrementFailedAttempt()
    {
        _failedAttempts.Inc();
        _notificationAttempts.WithLabels("failed").Inc();
    }

    public void IncrementExhausted() => _notificationsExhausted.Inc();

    public void SetStatusCount(string status, double count) => _notificationsByStatus.WithLabels(status).Set(count);
    public IDisposable StartDeliveryTimer() => _deliveryDuration.NewTimer();
    public void ObserveRetryCount(int retryCount) => _retryCountHistogram.Observe(retryCount);
}