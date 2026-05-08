namespace TaskFlow_API.Models;

/// <summary>
/// Tabla intermedia que rompe la relación N:M entre Task y Tag (RF-04.2).
/// Reemplaza al antiguo `TaskTag` para alinearse con la nomenclatura del
/// diagrama ER actualizado. La PK es compuesta `(TaskId, TagId)` para impedir
/// duplicados y simplificar el upsert al añadir/quitar etiquetas.
/// </summary>
public class TaskLabel
{
    public Guid TaskId { get; set; }
    public Task? Task { get; set; }

    public Guid TagId { get; set; }
    public Tag? Tag { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
