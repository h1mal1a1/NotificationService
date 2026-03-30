using Microsoft.EntityFrameworkCore;
using NotificationService.Data;

namespace NotificationService.Extensions;

public static class MigrationExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        const int maxAttempts = 10;
        var delay = TimeSpan.FromSeconds(5);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await using var scope = app.Services.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var logger = scope.ServiceProvider
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("MigrationExtensions");

                logger.LogInformation("Applying migrations. Attempt {Attempt}/{MaxAttempts}", attempt, maxAttempts);

                await dbContext.Database.MigrateAsync();

                logger.LogInformation("Migrations applied successfully.");
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                await using var scope = app.Services.CreateAsyncScope();
                var logger = scope.ServiceProvider
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("MigrationExtensions");

                logger.LogWarning(ex,
                    "Failed to apply migrations on attempt {Attempt}/{MaxAttempts}. Retrying in {DelaySeconds} seconds.",
                    attempt,
                    maxAttempts,
                    delay.TotalSeconds);

                await Task.Delay(delay);
            }
        }

        await using var finalScope = app.Services.CreateAsyncScope();
        var finalLogger = finalScope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("MigrationExtensions");

        finalLogger.LogError("Failed to apply migrations after all retry attempts.");

        throw new Exception("Failed to apply database migrations after multiple attempts.");
    }
}