using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NotificationService.Configuration;
using RabbitMQ.Client;

namespace NotificationService.Services.RabbitMq;

public class RabbitMqPublisher(IOptions<RabbitMqSettings> rabbitMqOptions) : IRabbitMqPublisher
{
    private readonly RabbitMqSettings _settings = rabbitMqOptions.Value;
    public async Task PublishAsync<T>(T message, string routingKey, CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory()
        {
            HostName = _settings.Host,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password
        };

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(exchange: _settings.Exchange, type: ExchangeType.Direct, durable: true,
            cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var properties = new BasicProperties() { Persistent = true };
        await channel.BasicPublishAsync(exchange: _settings.Exchange, routingKey: routingKey, mandatory: false,
            basicProperties: properties, body: body, cancellationToken: cancellationToken);
        
        
    }
}