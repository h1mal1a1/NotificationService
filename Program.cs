using NotificationService.Extensions;

namespace NotificationService;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services
            .AddAppSettings(builder.Configuration)
            .AddPostgres(builder.Configuration)
            .AddApplicationServices();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.MapControllers();

        await app.ApplyMigrationsAsync();
        await app.ApplySeedDataAsync();

        await app.RunAsync();
    }
}