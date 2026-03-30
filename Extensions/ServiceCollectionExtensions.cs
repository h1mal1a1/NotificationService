using Microsoft.EntityFrameworkCore;
using NotificationService.Configuration;
using NotificationService.Data;

namespace NotificationService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PostgreSqlSettings>(configuration.GetSection(PostgreSqlSettings.SectionName));
        services.Configure<RabbitMqSettings>(configuration.GetSection(RabbitMqSettings.SectionName));
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.Configure<RetrySettings>(configuration.GetSection(RetrySettings.SectionName));

        return services;
    }
    public static IServiceCollection AddPostgres(this IServiceCollection services, IConfiguration configuration)
    {
        var postgresSettings = configuration
                                   .GetSection(PostgreSqlSettings.SectionName)
                                   .Get<PostgreSqlSettings>()
                               ?? throw new Exception("PostgreSql settings not found");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(postgresSettings.ConnectionString));

        return services;
    }
}