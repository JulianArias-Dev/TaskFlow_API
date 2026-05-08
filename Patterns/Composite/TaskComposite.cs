using TaskEntity = TaskFlow_API.Models.Task;

namespace TaskFlow_API.Patterns.Composite;

/// <summary>
/// Composite — Composite (nodo con hijos).
/// Encapsula una tarea raíz que tiene subtareas. Su progreso es la media
/// ponderada de los progresos de sus hijos. Si una subtarea es a su vez
/// compuesta (subsubtareas), la recursión funciona de forma transparente.
/// </summary>
public class TaskComposite : ITaskComponent
{
    private readonly TaskEntity _task;
    private readonly List<ITaskComponent> _children = new();

    public TaskComposite(TaskEntity task) => _task = task;

    public Guid Id => _task.Id;
    public string Title => _task.Title;
    public int StatusId => _task.StatusId;
    public int Weight => 1;

    public IReadOnlyList<ITaskComponent> Children => _children;

    public void Add(ITaskComponent child) => _children.Add(child);
    public void Remove(ITaskComponent child) => _children.Remove(child);

    public int GetEstimatedHours()
        => _task.EstimatedHours + _children.Sum(c => c.GetEstimatedHours());

    public double GetProgress()
    {
        // Sin hijos, se comporta como una hoja.
        if (_children.Count == 0)
            return _task.StatusId == TaskLeaf.DoneStatusId ? 100.0 : 0.0;

        var totalWeight = _children.Sum(c => c.Weight);
        if (totalWeight == 0) return 0.0;

        var weightedSum = _children.Sum(c => c.GetProgress() * c.Weight);
        return Math.Round(weightedSum / totalWeight, 2);
    }

    public int CountAll() => 1 + _children.Sum(c => c.CountAll());
}

/// <summary>
/// Builder estático — convierte una entidad `Task` cargada con sus subtareas
/// (recursivamente) en el árbol Composite. Útil desde el TaskService al
/// devolver un detalle de tarea con su progreso global.
/// </summary>
public static class TaskCompositeBuilder
{
    public static ITaskComponent Build(TaskEntity task)
    {
        if (task.SubTasks is null || task.SubTasks.Count == 0)
            return new TaskLeaf(task);

        var composite = new TaskComposite(task);
        foreach (var sub in task.SubTasks)
            composite.Add(Build(sub));
        return composite;
    }
}
