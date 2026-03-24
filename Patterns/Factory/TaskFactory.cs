using TaskFlow_API.Models;
using TaskEntity = TaskFlow_API.Models.Task;
using TaskStatus = TaskFlow_API.Models.TaskStatus;
using TaskType = TaskFlow_API.Models.TaskType;
using TaskPriority = TaskFlow_API.Models.TaskPriority;

namespace TaskFlow_API.Patterns.Factory;

/// <summary>
/// Interfaz base para las diferentes implementaciones de Tasks
/// que heredan de TaskEntity según su tipo específico.
/// </summary>
public interface ITaskFactory
{
    TaskEntity CreateTask(string title, string description, Guid projectId);
}

/// <summary>
/// Factoría para crear tasks de tipo BUG
/// </summary>
public class BugTaskFactory : ITaskFactory
{
    public TaskEntity CreateTask(string title, string description, Guid projectId)
    {
        return new TaskEntity
        {
            Title = title,
            Description = description,
            Type = TaskType.BUG,
            Priority = TaskPriority.HIGH,
            Status = TaskStatus.TODO
        };
    }
}

/// <summary>
/// Factoría para crear tasks de tipo FEATURE
/// </summary>
public class FeatureTaskFactory : ITaskFactory
{
    public TaskEntity CreateTask(string title, string description, Guid projectId)
    {
        return new TaskEntity
        {
            Title = title,
            Description = description,
            Type = TaskType.FEATURE,
            Priority = TaskPriority.MEDIUM,
            Status = TaskStatus.TODO
        };
    }
}

/// <summary>
/// Factoría para crear tasks de tipo TASK
/// </summary>
public class SimpleTaskFactory : ITaskFactory
{
    public TaskEntity CreateTask(string title, string description, Guid projectId)
    {
        return new TaskEntity
        {
            Title = title,
            Description = description,
            Type = TaskType.TASK,
            Priority = TaskPriority.MEDIUM,
            Status = TaskStatus.TODO
        };
    }
}

/// <summary>
/// Factoría para crear tasks de tipo IMPROVEMENT
/// </summary>
public class ImprovementTaskFactory : ITaskFactory
{
    public TaskEntity CreateTask(string title, string description, Guid projectId)
    {
        return new TaskEntity
        {
            Title = title,
            Description = description,
            Type = TaskType.IMPROVEMENT,
            Priority = TaskPriority.LOW,
            Status = TaskStatus.TODO
        };
    }
}

/// <summary>
/// Factory Method: Factoría que centraliza la creación de tasks
/// según su tipo específico
/// </summary>
public class TaskFactoryProvider
{
    private readonly Dictionary<TaskType, ITaskFactory> _factories;

    public TaskFactoryProvider()
    {
        _factories = new Dictionary<TaskType, ITaskFactory>
        {
            { TaskType.BUG, new BugTaskFactory() },
            { TaskType.FEATURE, new FeatureTaskFactory() },
            { TaskType.TASK, new SimpleTaskFactory() },
            { TaskType.IMPROVEMENT, new ImprovementTaskFactory() }
        };
    }

    public TaskEntity CreateTask(TaskType taskType, string title, string description, Guid projectId)
    {
        if (!_factories.TryGetValue(taskType, out var factory))
        {
            throw new ArgumentException($"Unknown task type: {taskType}");
        }

        return factory.CreateTask(title, description, projectId);
    }
}
