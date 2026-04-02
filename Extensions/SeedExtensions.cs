using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Entities;

namespace NotificationService.Extensions;

public static class SeedExtensions
{
    public static async Task ApplySeedDataAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("SeedExtensions");

        await SeedNotificationTemplatesAsync(dbContext, logger);
    }

    private static async Task SeedNotificationTemplatesAsync(
        AppDbContext dbContext,
        ILogger logger)
    {
        const string templateCode = "password_reset";

        var exists = await dbContext.NotificationsTemplates
            .AnyAsync(x => x.TemplateCode == templateCode && x.Channel == NotificationChannel.Email);

        if (exists)
        {
            logger.LogInformation(
                "Notification template '{TemplateCode}' already exists. Seed skipped.",
                templateCode);

            return;
        }

        var template = new NotificationTemplate
        {
            TemplateCode = templateCode,
            Channel = NotificationChannel.Email,
            SubjectTemplate = "Восстановление пароля",
            BodyTemplate =
                "Здравствуйте, {{UserName}}!\n\n" +
                "Для восстановления пароля перейдите по ссылке:\n" +
                "{{ResetLink}}\n\n" +
                "Ссылка действует {{ExpirationMinutes}} минут.",
            IsActive = true
        };

        dbContext.NotificationsTemplates.Add(template);
        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Notification template '{TemplateCode}' was added successfully.",
            templateCode);
    }
}