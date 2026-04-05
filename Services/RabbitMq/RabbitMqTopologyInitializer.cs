using Microsoft.Extensions.Options;
using NotificationService.Configuration;
using RabbitMQ.Client;

namespace NotificationService.Services.RabbitMq;

public class RabbitMqTopologyInitializer(
    IOptions<RabbitMqSettings> rabbitMqOptions,
    ILogger<RabbitMqTopologyInitializer> logger)
{
    private readonly RabbitMqSettings _settings = rabbitMqOptions.Value;
    private readonly ILogger<RabbitMqTopologyInitializer> _logger = logger;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 10;
        var delay = TimeSpan.FromSeconds(5);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _settings.Host,
                    Port = _settings.Port,
                    UserName = _settings.UserName,
                    Password = _settings.Password
                };

                await using var connection = await factory.CreateConnectionAsync(cancellationToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

                await channel.ExchangeDeclareAsync(
                    exchange: _settings.Exchange,
                    type: ExchangeType.Direct,
                    durable: true,
                    cancellationToken: cancellationToken);

                await channel.QueueDeclareAsync(
                    queue: _settings.Queue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: cancellationToken);

                await channel.QueueBindAsync(
                    queue: _settings.Queue,
                    exchange: _settings.Exchange,
                    routingKey: _settings.RoutingKey,
                    cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "RabbitMQ topology initialized successfully on attempt {Attempt}/{MaxAttempts}.",
                    attempt,
                    maxAttempts);

                return;
            }
            catch (Exception ex)
            {
                if (attempt == maxAttempts)
                {
                    _logger.LogError(
                        ex,
                        "Failed to initialize RabbitMQ topology after {MaxAttempts} attempts.",
                        maxAttempts);

                    throw;
                }

                _logger.LogWarning(ex,"Failed to initialize RabbitMQ topology on attempt " +
                                      "{Attempt}/{MaxAttempts}. Retrying in {DelaySeconds} seconds.", attempt, 
                    maxAttempts, delay.TotalSeconds);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}