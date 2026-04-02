using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using NotificationService.Configuration;

namespace NotificationService.Services.Email;

public class SmtpEmailSender(IOptions<SmtpSettings> smtpOptions) : IEmailSender
{
    private readonly SmtpSettings _smtpSettings = smtpOptions.Value;

    public async Task SendAsync(string recipient, string subject, string body,
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromEmail));
        message.To.Add(MailboxAddress.Parse(recipient));
        message.Subject = subject;

        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        var secureSocketOptions = _smtpSettings.UseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;
        await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, secureSocketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_smtpSettings.UserName))
            await client.AuthenticateAsync(_smtpSettings.UserName, _smtpSettings.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}