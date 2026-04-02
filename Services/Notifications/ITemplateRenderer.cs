namespace NotificationService.Services.Notifications;

public interface ITemplateRenderer
{
    string Render(string template, IReadOnlyDictionary<string, string> values);
}