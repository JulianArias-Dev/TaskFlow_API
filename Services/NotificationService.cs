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

    public async System.Threading.Tasks.Task NotifyAsync(Guid userId, string subject, string content)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) return;

        var message = new NotificationMessage
        {
            UserId = userId,
            ToEmail = user.NotifyByEmail ? user.Email : null,
            Subject = subject,
            Content = content
        };

        // Itera los Adapters disponibles y envía por los que CanHandle.
        // Los Adapters están registrados en DI como IEnumerable<INotificationAdapter>.
        foreach (var adapter in _adapters)
        {
            if (!adapter.CanHandle(message)) continue;
            try
            {
                await adapter.SendAsync(message);
                _logger.LogDebug("Notification sent via {Channel} to user {UserId}", adapter.Channel, userId);
            }
            catch (Exception ex)
            {
                // Un canal fallando no debe tumbar otros canales (resiliencia).
                _logger.LogError(ex, "Adapter {Channel} failed sending notification to {UserId}", adapter.Channel, userId);
            }
        }
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
