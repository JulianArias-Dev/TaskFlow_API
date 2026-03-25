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
            TypeId = 2,
            PriorityId = 3,
            StatusId = 1
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
            TypeId = 2,
            PriorityId = 2,
            StatusId = 1
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
            TypeId = 5,
            PriorityId = 2,
            StatusId = 1
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
            TypeId = 3,
            PriorityId = 1,
            StatusId = 1 
        };
    }
}

/// <summary>
/// Factory Method: Factoría que centraliza la creación de tasks
/// según su tipo específico
/// </summary>
public class TaskFactoryProvider
{
	private readonly Dictionary<int, ITaskFactory> _factories;

	public TaskFactoryProvider()
	{
		_factories = new Dictionary<int, ITaskFactory>
		{
			{ 1, new SimpleTaskFactory() },      // ID 1: TASK
            { 2, new BugTaskFactory() },        // ID 2: BUG
            { 3, new FeatureTaskFactory() },    // ID 3: FEATURE
            { 4, new ImprovementTaskFactory() } // ID 4: IMPROVEMENT
        };
	}

	public TaskEntity CreateTask(int taskTypeId, string title, string description, Guid projectId)
	{
		if (!_factories.TryGetValue(taskTypeId, out var factory))
		{
			throw new ArgumentException($"Unknown task type ID: {taskTypeId}");
		}

		return factory.CreateTask(title, description, projectId);
	}
}
