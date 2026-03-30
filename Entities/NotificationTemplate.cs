namespace NotificationService.Entities;

public class NotificationTemplate
{
    public long Id { get; set; }
    public string TemplateCode { get; set; } = null!;
    public NotificationChannel Channel { get; set; }
    public string SubjectTemplate { get; set; } = null!;
    public string BodyTemplate { get; set; } = null!;
    public bool IsActive { get; set; }
}