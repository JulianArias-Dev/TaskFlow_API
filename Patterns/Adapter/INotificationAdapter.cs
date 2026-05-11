namespace TaskFlow_API.Patterns.Adapter;

/// <summary>
/// PATRÓN ADAPTER — Target.
///
/// Interfaz uniforme que la app utiliza para enviar notificaciones, sin
/// importar el canal subyacente (Email SMTP, SendGrid, In-App, Slack, SMS...).
/// Cada canal externo se integra mediante una clase Adapter que traduce
/// el contrato `NotificationMessage` al SDK/protocolo específico del proveedor.
/// </summary>
public interface INotificationAdapter
{
    /// <summary>Identificador del canal — útil para logging y filtrado por preferencia de usuario.</summary>
    string Channel { get; }

    /// <summary>¿Este adapter puede manejar este mensaje? (p.ej. Email requiere `ToEmail`).</summary>
    bool CanHandle(NotificationMessage message);

    /// <summary>Envía la notificación traduciendo el mensaje al formato del proveedor.</summary>
    System.Threading.Tasks.Task SendAsync(NotificationMessage message);
}

/// <summary>
/// Mensaje neutro de la aplicación — no conoce ningún proveedor concreto.
/// Los Adapter son los que lo transforman al payload específico (MimeMessage, JSON SendGrid, etc.).
/// </summary>
public class NotificationMessage
{
    public Guid UserId { get; set; }
    public string? ToEmail { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Código del evento que dispara la notificación (ASSIGNED, DUE_OVERDUE,
    /// COMMENT, STATUS_CHANGE, …). Permite al servicio resolver las
    /// preferencias del usuario antes de elegir canales.
    /// </summary>
    public string? EventCode { get; set; }
}

public enum NotificationPriority { Low, Normal, High }
