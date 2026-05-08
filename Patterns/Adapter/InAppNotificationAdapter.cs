using TaskFlow_API.Models;
using TaskFlow_API.Repositories;

namespace TaskFlow_API.Patterns.Adapter;

/// <summary>
/// Adapter (concreto) — In-App.
/// Adapta el envío de notificaciones a la persistencia interna: una fila en la tabla
/// `Notifications` que el frontend consume vía `/api/Notifications`.
/// </summary>
public class InAppNotificationAdapter : INotificationAdapter
{
    private readonly IUnitOfWork _uow;

    public InAppNotificationAdapter(IUnitOfWork uow) => _uow = uow;

    public string Channel => "InApp";

    public bool CanHandle(NotificationMessage message) => message.UserId != Guid.Empty;

    public async System.Threading.Tasks.Task SendAsync(NotificationMessage message)
    {
        // Adapter: traducimos NotificationMessage -> entidad Notification interna.
        var notification = new Notification
        {
            UserId = message.UserId,
            Subject = message.Subject,
            Content = message.Content,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        await _uow.Notifications.AddAsync(notification);
        await _uow.SaveChangesAsync();
    }
}
