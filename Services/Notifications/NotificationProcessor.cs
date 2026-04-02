using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NotificationService.Configuration;
using NotificationService.Contracts.Events;
using NotificationService.Data;
using NotificationService.Entities;
using NotificationService.Services.Email;

namespace NotificationService.Services.Notifications;

public class NotificationProcessor(
    AppDbContext dbContext,
    ITemplateRenderer templateRenderer,
    IEmailSender emailSender,
    IOptions<RetrySettings> retryOptions,
    ILogger<NotificationProcessor> logger) : INotificationProcessor
{
    private const string PasswordResetTemplateCode = "password_reset";

    private readonly AppDbContext _dbContext = dbContext;
    private readonly ITemplateRenderer _templateRenderer = templateRenderer;
    private readonly IEmailSender _emailSender = emailSender;
    private readonly RetrySettings _retrySettings = retryOptions.Value;
    private readonly ILogger<NotificationProcessor> _logger = logger;

    public async Task<long> ProcessPasswordResetAsync(PasswordResetRequestedEvent notificationEvent,
        CancellationToken cancellationToken = default)
    {
        var template = await _dbContext.NotificationsTemplates
            .FirstOrDefaultAsync(
                x => x.TemplateCode == PasswordResetTemplateCode && x.Channel == NotificationChannel.Email &&
                     x.IsActive, cancellationToken);
        if (template is null)
            throw new InvalidOperationException(
                $"Active template '{PasswordResetTemplateCode}' for Email channel was not found");

        var templateValues = new Dictionary<string, string>()
        {
            ["UserName"] = notificationEvent.UserName,
            ["ResetLink"] = notificationEvent.ResetLink,
            ["ExpirationMinutes"] = notificationEvent.ExpirationMinutes.ToString()
        };

        var subject = _templateRenderer.Render(template.SubjectTemplate, templateValues);
        var body = _templateRenderer.Render(template.BodyTemplate, templateValues);

        var notification = new Notification()
        {
            EventType = nameof(PasswordResetRequestedEvent),
            Channel = NotificationChannel.Email,
            Recipient = notificationEvent.Email,
            Subject = subject,
            Body = body,
            Status = NotificationStatus.Pending,
            RetryCount = 0,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var maxAttempts = Math.Max(1, _retrySettings.MaxAttempts);
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                notification.Status = NotificationStatus.Processing;
                await _dbContext.SaveChangesAsync(cancellationToken);
                await _emailSender.SendAsync(notification.Recipient, notification.Subject, notification.Body,
                    cancellationToken);

                notification.Status = NotificationStatus.Sent;
                notification.SentAtUtc = DateTime.UtcNow;
                notification.LastError = null;
                notification.RetryCount = attempt - 1;

                _dbContext.NotificationsAttempts.Add(new NotificationAttempt()
                {
                    IdNotification = notification.Id,
                    AttemptNumber = attempt,
                    AttemptedAtUtc = DateTime.UtcNow,
                    IsSuccess = true,
                    ErrorMessage = null
                });

                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Notification {NotificationId} aws sent successfully on attempt {Attempt}",
                    notification.Id, attempt);
                return notification.Id;
            }
            catch (Exception ex)
            {
                notification.RetryCount = attempt;
                notification.LastError = ex.Message;
                notification.Status = attempt < maxAttempts
                    ? NotificationStatus.Pending
                    : NotificationStatus.Failed;

                _dbContext.NotificationsAttempts.Add(new NotificationAttempt()
                {
                    IdNotification = notification.Id,
                    AttemptNumber = attempt,
                    AttemptedAtUtc = DateTime.UtcNow,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                });

                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogWarning(ex,
                    "Failed to send notification {NotificationId} on attempt {Attempt}/{MaxAttempts}.",
                    notification.Id,
                    attempt,
                    maxAttempts
                );

                if (attempt >= maxAttempts)
                    break;
                var delay = TimeSpan.FromSeconds(Math.Max(1, _retrySettings.DelaySeconds));
                await Task.Delay(delay, cancellationToken);
            }
        }

        return notification.Id;
    }
}