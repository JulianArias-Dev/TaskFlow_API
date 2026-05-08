namespace TaskFlow_API.Models;

/// <summary>
/// Filtro guardado por el usuario (RF-07.3 — Búsqueda y Filtros).
/// `FilterCriteria` se persiste como JSON serializado.
/// </summary>
public class SavedFilter
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>JSON serializado con los criterios (status, priority, tags, etc.).</summary>
    public string FilterCriteria { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
