namespace NotificationService.Configuration;

public class RabbitMqSettings
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = null!;
    public int Port { get; set; }
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string Exchange { get; set; } = null!;
    public string Queue { get; set; } = null!;
    public string RoutingKey { get; set; } = null!;
}