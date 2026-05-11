using System.Text.Json;
using TaskFlow_API.DTOs;
using TaskFlow_API.Patterns.Adapter;
using TaskFlow_API.Repositories;

namespace TaskFlow_API.Services;

public class EmailSettings
{
    public required string From { get; set; }
    public required string SmtpServer { get; set; }
    public required string Port { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }

    /// <summary>Proveedor de email activo: "Smtp" (default) o "SendGrid".</summary>
    public string Provider { get; set; } = "Smtp";
}

public interface INotificationService
{
    System.Threading.Tasks.Task NotifyAsync(Guid userId, string subject, string content);

    /// <summary>
    /// Versión consciente del tipo de evento — antes de enviar, consulta las
    /// preferencias por canal del usuario (RF-05.3) y desactiva los adapters
    /// que el usuario haya silenciado para ese tipo de evento.
    /// </summary>
    System.Threading.Tasks.Task NotifyAsync(Guid userId, string eventCode, string subject, string content);

    System.Threading.Tasks.Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userId);
    System.Threading.Tasks.Task<bool> MarkAsReadAsync(Guid notificationId);
}

/// <summary>
/// Servicio orquestador. NO conoce los detalles de envío — delega a los Adapters.
/// Esto demuestra el patrón Adapter: el cliente (NotificationService) sólo
/// habla con la abstracción `INotificationAdapter`.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEnumerable<INotificationAdapter> _adapters;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IUnitOfWork unitOfWork,
        IEnumerable<INotificationAdapter> adapters,
        ILogger<NotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _adapters = adapters;
        _logger = logger;
    }

    public System.Threading.Tasks.Task NotifyAsync(Guid userId, string subject, string content)
        => NotifyAsync(userId, eventCode: string.Empty, subject, content);

    public async System.Threading.Tasks.Task NotifyAsync(Guid userId, string eventCode, string subject, string content)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) return;

        // Resolver preferencias del usuario (RF-05.3). Si el evento o las
        // preferencias no existen, default = todos los canales activos.
        var (allowInApp, allowEmail) = ResolveChannelPreferences(user, eventCode);

        var message = new NotificationMessage
        {
            UserId = userId,
            EventCode = string.IsNullOrWhiteSpace(eventCode) ? null : eventCode,
            ToEmail = (allowEmail && user.NotifyByEmail) ? user.Email : null,
            Subject = subject,
            Content = content
        };

        foreach (var adapter in _adapters)
        {
            if (!adapter.CanHandle(message)) continue;

            // Filtrado adicional por preferencia del usuario.
            if (adapter.Channel == "InApp" && !allowInApp) continue;

            try
            {
                await adapter.SendAsync(message);
                _logger.LogDebug("Notification sent via {Channel} to user {UserId} (event={EventCode})",
                    adapter.Channel, userId, eventCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Adapter {Channel} failed sending notification to {UserId}", adapter.Channel, userId);
            }
        }
    }

    /// <summary>
    /// Consulta el JSON `NotificationPreferences` del User y devuelve un par
    /// (inApp, email) para el evento dado. Cualquier ausencia se trata como
    /// "permitido" (default opt-in).
    /// </summary>
    private static (bool inApp, bool email) ResolveChannelPreferences(Models.User user, string eventCode)
    {
        if (string.IsNullOrWhiteSpace(eventCode) || string.IsNullOrWhiteSpace(user.NotificationPreferences))
            return (true, true);

        try
        {
            var prefs = JsonSerializer.Deserialize<Dictionary<string, NotificationChannelPreferenceDto>>(
                user.NotificationPreferences,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (prefs != null && prefs.TryGetValue(eventCode.Trim().ToUpperInvariant(), out var v))
                return (v.InApp, v.Email);
        }
        catch (JsonException) { /* JSON corrupto — default opt-in. */ }

        return (true, true);
    }

    public async System.Threading.Tasks.Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userId)
    {
        var allNotifications = await _unitOfWork.Notifications.GetAllAsync();

        return allNotifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(30)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Message = n.Content,
                Type = n.Subject,
                Content = n.Content,
                CreatedAt = n.CreatedAt,
                IsRead = n.IsRead
            })
            .ToList();
    }

    public async System.Threading.Tasks.Task<bool> MarkAsReadAsync(Guid notificationId)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
        if (notification == null) return false;

        notification.IsRead = true;
        return await _unitOfWork.SaveChangesAsync() > 0;
    }
}
