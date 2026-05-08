namespace TaskFlow_API.Patterns.Decorator;

/// <summary>
/// PATRÓN DECORATOR — Component.
///
/// Vista enriquecida de una tarea para el tablero Kanban. La idea es poder
/// añadir responsabilidades (etiquetas, indicadores de adjuntos, badge de
/// vencida, color por prioridad...) SIN modificar la clase base ni heredar
/// para cada combinación posible. Cada Decorator envuelve un componente
/// y agrega información a `Render()`.
///
/// El cliente del controller llama `Render()` y obtiene un objeto con
/// todos los enriquecimientos aplicados — sin saber cuántos decoradores hay.
/// </summary>
public interface ITaskView
{
    Guid TaskId { get; }
    TaskViewModel Render();
}

/// <summary>Modelo plano que se serializa al frontend.</summary>
public class TaskViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public int PriorityId { get; set; }

    // Campos que cada Decorator activa según su responsabilidad.
    public List<string> Labels { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public int AttachmentCount { get; set; }
    public bool IsOverdue { get; set; }
    public string? PriorityColor { get; set; }
    public string? Badge { get; set; }
}
