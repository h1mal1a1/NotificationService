namespace NotificationService.Services.RabbitMq;

public interface IRabbitMqPublisher
{
    Task PublishAsync<T>(T message, string routingKey, CancellationToken cancellationToken = default);
}