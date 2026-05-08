using TaskEntity = TaskFlow_API.Models.Task;

namespace TaskFlow_API.Patterns.Decorator;

/// <summary>
/// Abstract Decorator — referencia el componente envuelto y delega lo que no enriquece.
/// </summary>
public abstract class TaskViewDecorator : ITaskView
{
    protected readonly ITaskView Inner;

    protected TaskViewDecorator(ITaskView inner) => Inner = inner;

    public Guid TaskId => Inner.TaskId;

    public abstract TaskViewModel Render();
}

/// <summary>
/// Concrete Decorator — añade Tags y Labels (etiquetas N:M vía la tabla `TaskLabels`).
/// </summary>
public class TagsDecorator : TaskViewDecorator
{
    private readonly TaskEntity _task;

    public TagsDecorator(ITaskView inner, TaskEntity task) : base(inner) => _task = task;

    public override TaskViewModel Render()
    {
        var view = Inner.Render();

        // 1) Tags inline (lista de strings en la entidad)
        if (_task.Tags is { Count: > 0 })
            view.Tags.AddRange(_task.Tags);

        // 2) Labels normalizados (relación TaskLabel -> Tag)
        if (_task.TaskLabels is { Count: > 0 })
        {
            view.Labels.AddRange(
                _task.TaskLabels
                     .Where(tl => tl.Tag != null)
                     .Select(tl => tl.Tag!.Name));
        }

        return view;
    }
}

/// <summary>
/// Concrete Decorator — indica cuántos archivos adjuntos tiene la tarea (icono 📎).
/// </summary>
public class AttachmentsDecorator : TaskViewDecorator
{
    private readonly int _attachmentCount;

    public AttachmentsDecorator(ITaskView inner, int attachmentCount) : base(inner)
        => _attachmentCount = attachmentCount;

    public override TaskViewModel Render()
    {
        var view = Inner.Render();
        view.AttachmentCount = _attachmentCount;
        return view;
    }
}

/// <summary>
/// Concrete Decorator — marca la tarea como vencida si DueDate &lt; ahora y no está Done.
/// </summary>
public class OverdueDecorator : TaskViewDecorator
{
    private readonly TaskEntity _task;

    public OverdueDecorator(ITaskView inner, TaskEntity task) : base(inner) => _task = task;

    public override TaskViewModel Render()
    {
        var view = Inner.Render();
        view.IsOverdue = _task.DueDate.HasValue
                      && _task.DueDate.Value < DateTime.UtcNow
                      && _task.StatusId != 3 /* Done */;
        if (view.IsOverdue) view.Badge = "OVERDUE";
        return view;
    }
}

/// <summary>
/// Concrete Decorator — color visual según prioridad (LOW/MEDIUM/HIGH/CRITICAL).
/// </summary>
public class PriorityColorDecorator : TaskViewDecorator
{
    public PriorityColorDecorator(ITaskView inner) : base(inner) { }

    public override TaskViewModel Render()
    {
        var view = Inner.Render();
        view.PriorityColor = view.PriorityId switch
        {
            1 => "#10b981", // LOW — green
            2 => "#3b82f6", // MEDIUM — blue
            3 => "#f59e0b", // HIGH — amber
            4 => "#ef4444", // CRITICAL — red
            _ => "#9ca3af"
        };
        return view;
    }
}
