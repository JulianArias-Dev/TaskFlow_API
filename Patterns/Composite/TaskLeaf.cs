using TaskEntity = TaskFlow_API.Models.Task;

namespace TaskFlow_API.Patterns.Composite;

/// <summary>
/// Composite — Leaf. Tarea atómica sin subtareas.
/// Su progreso es binario: 100% si está Done, 0% en cualquier otro caso.
/// </summary>
public class TaskLeaf : ITaskComponent
{
    private readonly TaskEntity _task;

    /// <summary>Id del estado "Done" — debe coincidir con el seed en el DbContext.</summary>
    public const int DoneStatusId = 3;

    public TaskLeaf(TaskEntity task) => _task = task;

    public Guid Id => _task.Id;
    public string Title => _task.Title;
    public int StatusId => _task.StatusId;
    public int Weight => 1;

    public int GetEstimatedHours() => _task.EstimatedHours;

    public double GetProgress() => _task.StatusId == DoneStatusId ? 100.0 : 0.0;

    public int CountAll() => 1;
}
