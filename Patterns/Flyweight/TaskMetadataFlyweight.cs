namespace TaskFlow_API.Patterns.Flyweight;

/// <summary>
/// PATRÓN FLYWEIGHT — Flyweight (estado intrínseco compartido).
///
/// Cuando devolvemos listas grandes de tareas al frontend (Kanban, búsquedas,
/// reportes), cada tarea repite los mismos metadatos: nombre del tipo, color,
/// icono, tooltip, prioridad, etc. Multiplicar esos strings por miles de
/// tareas malgasta memoria y ancho de banda.
///
/// Este Flyweight cachea UNA instancia inmutable por (Kind + Id) y la reusa
/// en TODAS las tareas que la referencian. El "estado extrínseco" (el TaskId
/// concreto, sus assignments, etc.) sigue siendo único por tarea.
/// </summary>
public sealed class TaskMetadataFlyweight
{
    public string Kind { get; }
    public int Id { get; }
    public string Name { get; }
    public string Color { get; }
    public string Icon { get; }
    public string Tooltip { get; }

    internal TaskMetadataFlyweight(string kind, int id, string name, string color, string icon, string tooltip)
    {
        Kind = kind;
        Id = id;
        Name = name;
        Color = color;
        Icon = icon;
        Tooltip = tooltip;
    }
}
