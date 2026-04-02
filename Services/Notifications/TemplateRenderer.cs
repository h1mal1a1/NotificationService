namespace NotificationService.Services.Notifications;

public class TemplateRenderer : ITemplateRenderer
{
    public string Render(string template, IReadOnlyDictionary<string, string> values)
    {
        if (string.IsNullOrWhiteSpace(template))
            return string.Empty;
        var result = template;
        foreach (var pair in values)
        {
            var placeholder = "{{" + pair.Key + "}}";
            result = result.Replace(placeholder, pair.Value);
        }

        return result;
    }
}