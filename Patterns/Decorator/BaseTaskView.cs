using TaskEntity = TaskFlow_API.Models.Task;

namespace TaskFlow_API.Patterns.Decorator;

/// <summary>
/// Concrete Component — vista base de una tarea, sin enriquecimientos.
/// </summary>
public class BaseTaskView : ITaskView
{
    private readonly TaskEntity _task;

    public BaseTaskView(TaskEntity task) => _task = task;

    public Guid TaskId => _task.Id;

    public TaskViewModel Render() => new()
    {
        Id = _task.Id,
        Title = _task.Title,
        Description = _task.Description,
        StatusId = _task.StatusId,
        PriorityId = _task.PriorityId
    };
}
