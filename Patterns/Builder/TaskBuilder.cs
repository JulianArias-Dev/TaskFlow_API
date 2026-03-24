using TaskFlow_API.Models;
using TaskEntity = TaskFlow_API.Models.Task;
using TaskStatus = TaskFlow_API.Models.TaskStatus;
using TaskType = TaskFlow_API.Models.TaskType;
using TaskPriority = TaskFlow_API.Models.TaskPriority;

namespace TaskFlow_API.Patterns.Builder;

/// <summary>
/// Patrón Builder: Permite la construcción paso a paso de una tarea
/// con configuración detallada y flexible
/// </summary>
public class TaskBuilder
{
    private TaskEntity _task;

    public TaskBuilder(Guid projectId, Guid columnId)
    {
        _task = new TaskEntity
        {
            ColumnId = columnId,
            Status = TaskStatus.TODO,
            Priority = TaskPriority.MEDIUM,
            Type = TaskType.TASK
        };
    }

    public TaskBuilder WithTitle(string title)
    {
        _task.Title = title;
        return this;
    }

    public TaskBuilder WithDescription(string description)
    {
        _task.Description = description;
        return this;
    }

    public TaskBuilder WithType(TaskType type)
    {
        _task.Type = type;
        return this;
    }

    public TaskBuilder WithStatus(TaskStatus status)
    {
        _task.Status = status;
        return this;
    }

    public TaskBuilder WithPriority(TaskPriority priority)
    {
        _task.Priority = priority;
        return this;
    }

	public TaskBuilder WithAssignee(Guid userId)
	{
		// Evitamos duplicados si se llama al mismo ID dos veces
		if (!_task.Assignments.Any(a => a.UserId == userId))
		{
			_task.Assignments.Add(new TaskAssignment
			{
				TaskId = _task.Id,
				UserId = userId,
				AssignedAt = DateTime.UtcNow
			});
		}
		return this;
	}

	// Opcional: Permitir una lista de golpe
	public TaskBuilder WithAssignees(IEnumerable<Guid> userIds)
	{
		foreach (var id in userIds)
		{
			WithAssignee(id);
		}
		return this;
	}

	public TaskBuilder WithEstimatedHours(int hours)
    {
        _task.EstimatedHours = hours;
        return this;
    }

    public TaskBuilder WithDueDate(DateTime dueDate)
    {
        _task.DueDate = dueDate;
        return this;
    }

    public TaskBuilder WithTag(string tag)
    {
        _task.Tags.Add(tag);
        return this;
    }

    public TaskBuilder WithTags(params string[] tags)
    {
        _task.Tags.AddRange(tags);
        return this;
    }

    public TaskBuilder AsSubtask(Guid parentTaskId)
    {
        _task.ParentTaskId = parentTaskId;
        _task.Type = TaskType.SUBTASK;
        return this;
    }

    public TaskBuilder ClearTags()
    {
        _task.Tags.Clear();
        return this;
    }

    public TaskEntity Build()
    {
        if (string.IsNullOrWhiteSpace(_task.Title))
        {
            throw new InvalidOperationException("Task title is required");
        }
        
        if (_task.ColumnId == Guid.Empty)
        {
            throw new InvalidOperationException("Column ID is required");
        }

        return _task;
    }

    public TaskEntity BuildAndReset(Guid projectId, Guid columnId)
    {
        var builtTask = Build();
        ResetBuilder(projectId, columnId);
        return builtTask;
    }

    private void ResetBuilder(Guid projectId, Guid columnId)
    {
        _task = new TaskEntity
        {
            ColumnId = columnId,
            Status = TaskStatus.TODO,
            Priority = TaskPriority.MEDIUM,
            Type = TaskType.TASK
        };
    }
}

/// <summary>
/// Builder para construir proyectos de forma escalonada
/// </summary>
public class ProjectBuilder
{
    private Project _project;

    public ProjectBuilder(Guid ownerId)
    {
        _project = new Project
        {
            OwnerId = ownerId
        };
    }

    public ProjectBuilder WithName(string name)
    {
        _project.Name = name;
        return this;
    }

    public ProjectBuilder WithDescription(string description)
    {
        _project.Description = description;
        return this;
    }

    public ProjectBuilder WithColor(string color)
    {
        _project.Color = color;
        return this;
    }

    public ProjectBuilder WithStartDate(DateTime startDate)
    {
        _project.StartDate = startDate;
        return this;
    }

    public ProjectBuilder WithEndDate(DateTime endDate)
    {
        _project.EndDate = endDate;
        return this;
    }

    public ProjectBuilder WithStatus(ProjectStatus status)
    {
        _project.Status = status;
        return this;
    }

    public Project Build()
    {
        if (string.IsNullOrWhiteSpace(_project.Name))
        {
            throw new InvalidOperationException("Project name is required");
        }

        if (_project.OwnerId == Guid.Empty)
        {
            throw new InvalidOperationException("Owner ID is required");
        }

        return _project;
    }

    public Project BuildAndReset(Guid ownerId)
    {
        var builtProject = Build();
        ResetBuilder(ownerId);
        return builtProject;
    }

    private void ResetBuilder(Guid ownerId)
    {
        _project = new Project
        {
            OwnerId = ownerId
        };
    }
}
