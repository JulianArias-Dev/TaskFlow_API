using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using TaskFlow_API.Services;

namespace TaskFlow_API.Patterns.Adapter;

/// <summary>
/// Adapter (concreto) — SMTP/Gmail vía MailKit.
/// Adapta `NotificationMessage` (formato neutro) al `MimeMessage` que MailKit espera.
/// La app NO depende de MimeMessage — sólo de `INotificationAdapter`.
/// </summary>
public class SmtpEmailNotificationAdapter : INotificationAdapter
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<SmtpEmailNotificationAdapter> _logger;

    public SmtpEmailNotificationAdapter(
        IOptions<EmailSettings> options,
        ILogger<SmtpEmailNotificationAdapter> logger)
    {
        _emailSettings = options.Value;
        _logger = logger;
    }

    public string Channel => "SmtpEmail";

    public bool CanHandle(NotificationMessage message)
        => !string.IsNullOrWhiteSpace(message.ToEmail);

    public async System.Threading.Tasks.Task SendAsync(NotificationMessage message)
    {
        if (!CanHandle(message))
        {
            _logger.LogWarning("SmtpEmailNotificationAdapter skipped — no ToEmail provided");
            return;
        }

        // Aquí ocurre la "adaptación": NotificationMessage -> MimeMessage
        var mime = new MimeMessage();
        mime.From.Add(MailboxAddress.Parse(_emailSettings.From));
        mime.To.Add(new MailboxAddress(string.Empty, message.ToEmail));
        mime.Subject = $"[{message.Priority}] {message.Subject}";
        mime.Body = new TextPart("html") { Text = message.Content };

        // Headers extra desde Metadata (ej. X-TaskFlow-TaskId)
        foreach (var kvp in message.Metadata)
            mime.Headers.Add($"X-TaskFlow-{kvp.Key}", kvp.Value);

        using var client = new SmtpClient();
        await client.ConnectAsync(_emailSettings.SmtpServer, int.Parse(_emailSettings.Port), true);
        await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
        await client.SendAsync(mime);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Email notification sent to {Email}", message.ToEmail);
    }
}
