namespace TaskFlow_API.Models;

/// <summary>
/// Tabla unificada para auditoría de cambios en todas las entidades
/// Sin FK estrictas para mantener histórico incluso si se elimina la entidad
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // Información de la entidad modificada
    public string EntityType { get; set; } = string.Empty; // Nombre de la clase (Task, Project, User, etc.)
    public Guid EntityId { get; set; }
    
    // Quién hizo el cambio
    public Guid? UserId { get; set; } // Nullable para cambios del sistema
    public User? User { get; set; } // Relación opcional para obtener detalles del usuario
	public string? UserName { get; set; } // Nombre del usuario para referencia (sin FK)
    
    // Qué acción se realizó
    public string Action { get; set; } = string.Empty; // Created, Updated, Deleted
    
    // Datos antes y después
    public string? OldData { get; set; } // JSON serializado del estado anterior
    public string? NewData { get; set; } // JSON serializado del estado nuevo
    
    // Metadatos
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Details { get; set; } // Campo adicional para detalles
}
