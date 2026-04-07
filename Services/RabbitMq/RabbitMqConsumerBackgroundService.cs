using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NotificationService.Configuration;
using NotificationService.Contracts.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService.Services.RabbitMq;

public class RabbitMqConsumerBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqSettings> rabbitMqOptions,
    ILogger<RabbitMqConsumerBackgroundService> logger) : BackgroundService
{
    private readonly RabbitMqSettings _settings = rabbitMqOptions.Value;
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory()
        {
            HostName = _settings.Host,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password
        };
        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            var body = eventArgs.Body.ToArray();
            var messageJson = Encoding.UTF8.GetString(body);

            try
            {
                var message = JsonSerializer.Deserialize<PasswordResetRequestedEvent>(messageJson);
                if (message is null)
                    throw new InvalidOperationException("Failed to deserialize RabbitMQ message.");
                
                await using var scope = scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<RabbitMqMessageHandler>();
                
                await handler.HandlePasswordResetAsync(message, stoppingToken);
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: stoppingToken);

                logger.LogInformation("RabbitMQ message acknowledged. DeliveryTag: {DeliveryTag}",
                    eventArgs.DeliveryTag);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while processing RabbitMq message. DeliveryTag: {DeliveryTag}",
                    eventArgs.DeliveryTag);
                await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false,
                    cancellationToken: stoppingToken);
            }
        };
        await _channel.BasicConsumeAsync(queue: _settings.Queue, autoAck: false, consumer: consumer,
            cancellationToken: stoppingToken);
        logger.LogInformation("RabbitMQ consumer started. Queue: {Queue}", _settings.Queue);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}