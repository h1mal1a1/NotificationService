namespace NotificationService.Configuration;

public class PostgreSqlSettings
{
    public const string SectionName = "PostgreSql";
    public string ConnectionString { get; set; } = null!;
}