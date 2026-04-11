using NotificationService.Extensions;
using NotificationService.Services.RabbitMq;

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
            .AddApplicationServices()
            .AddRabbitMq();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseHttpsRedirection();
        }


        app.MapControllers();

        await app.ApplyMigrationsAsync();
        await app.ApplySeedDataAsync();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var topologyInitializer = scope.ServiceProvider.GetRequiredService<RabbitMqTopologyInitializer>();
            await topologyInitializer.InitializeAsync();
        }

        await app.RunAsync();
    }
}