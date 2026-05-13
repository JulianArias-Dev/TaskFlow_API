namespace TaskFlow_API.Patterns.Adapter;

/// <summary>
/// SDK ficticio de un proveedor externo (SendGrid).
/// En un proyecto real esto sería un NuGet (`SendGrid.SendGridClient`).
/// Sirve para demostrar la "Adaptee" del patrón Adapter — su API es incompatible
/// con la del sistema (acepta un payload con `RecipientAddress`, `MailSubject`,
/// `BodyHtml`, etc., diferente al `NotificationMessage` neutro).
/// </summary>
public sealed class FakeSendGridClient
{
    private readonly ILogger _logger;

    public FakeSendGridClient(ILogger logger) => _logger = logger;

    public System.Threading.Tasks.Task<int> SendMailAsync(SendGridPayload payload)
    {
        // Simulamos llamada HTTP al endpoint v3/mail/send
        _logger.LogInformation(
            "[FAKE-SENDGRID] -> POST https://api.sendgrid.com/v3/mail/send | to={To} subject={Subject}",
            payload.RecipientAddress, payload.MailSubject);
        return System.Threading.Tasks.Task.FromResult(202); // Accepted
    }
}

public sealed class SendGridPayload
{
    public string RecipientAddress { get; set; } = string.Empty;
    public string MailSubject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "noreply@taskflow.app";
    public Dictionary<string, string> CustomArgs { get; set; } = new();
}

/// <summary>
/// Adapter (concreto) — SendGrid externo.
///
/// Aquí brilla el patrón Adapter: nuestro dominio sólo conoce `NotificationMessage`,
/// pero el SDK externo exige `SendGridPayload`. Este adaptador hace la traducción
/// SIN tocar ni el dominio (NotificationMessage) ni el SDK externo (FakeSendGridClient).
///
/// Si mañana migramos a Mailgun, AWS SES o Postmark, sólo creamos otro Adapter —
/// el resto del sistema queda intacto. (Open/Closed Principle.)
/// </summary>
public class SendGridNotificationAdapter : INotificationAdapter
{
    private readonly FakeSendGridClient _client;
    private readonly ILogger<SendGridNotificationAdapter> _logger;

    public SendGridNotificationAdapter(ILogger<SendGridNotificationAdapter> logger)
    {
        _logger = logger;
        _client = new FakeSendGridClient(logger);
    }

    public string Channel => "SendGrid";

    public bool CanHandle(NotificationMessage message) => !string.IsNullOrEmpty(message.ToEmail);

    public async System.Threading.Tasks.Task SendAsync(NotificationMessage message)
    {
        // Traducción: dominio neutro -> formato del proveedor externo.
        var payload = new SendGridPayload
        {
            RecipientAddress = message.ToEmail!,
            MailSubject = message.Subject,
            BodyHtml = WrapInTemplate(message),
            CustomArgs =
            {
                ["userId"] = message.UserId.ToString(),
                ["priority"] = message.Priority.ToString(),
                ["source"] = "taskflow-api"
            }
        };

        var statusCode = await _client.SendMailAsync(payload);
        if (statusCode is < 200 or >= 300)
            _logger.LogWarning("SendGrid returned non-success status {Status}", statusCode);
    }

    private static string WrapInTemplate(NotificationMessage message)
    {
        return NotificationContentFormatter.FormatAsHtml(message.Content);
    }
}
