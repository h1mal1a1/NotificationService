namespace NotificationService.Configuration;

public class SmtpSettings
{
    public const string SectionName = "Smtp";
    public string Host { get; set; } = null!;
    public int Port { get; set; } 
    public bool UseSsl { get; set; }
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FromEmail { get; set; } = null!;
    public string FromName { get; set; } = null!;
}