using TaskFlow_API.DTOs;
using TaskFlow_API.Models;
using TaskFlow_API.Repositories;
using TaskFlow_API.Patterns.Builder;
using TaskFlow_API.Patterns.Factory;
using TaskFlow_API.Patterns.Prototype;
using TaskEntity = TaskFlow_API.Models.Task;
using TaskStatus = TaskFlow_API.Models.TaskStatus;
using TaskType = TaskFlow_API.Models.TaskType;
using TaskPriority = TaskFlow_API.Models.TaskPriority;

namespace TaskFlow_API.Services;

/// <summary>
/// Interfaz del servicio de Task
/// Define operaciones de negocio para tareas
/// </summary>
public interface ITaskService
{
    System.Threading.Tasks.Task<TaskDto?> GetTaskByIdAsync(Guid id);
    System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetAllTasksAsync();
    System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetTasksByProjectAsync(Guid projectId);
    System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetTasksByStatusAsync(string status);
    System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetTasksByAssigneeAsync(Guid userId);
    System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetOverdueTasksAsync();
    System.Threading.Tasks.Task<TaskDto> CreateTaskAsync(CreateTaskDto createTaskDto);
    System.Threading.Tasks.Task<TaskDto> CreateTaskByTypeAsync(string taskType, string title, string description, Guid projectId);
    System.Threading.Tasks.Task<TaskDto> UpdateTaskAsync(Guid id, UpdateTaskDto updateTaskDto);
    System.Threading.Tasks.Task<bool> DeleteTaskAsync(Guid id);
    System.Threading.Tasks.Task<TaskDto?> CloneTaskAsync(Guid taskId);
    System.Threading.Tasks.Task<int> GetTaskCountByProjectAsync(Guid projectId);
    System.Threading.Tasks.Task<int> GetTaskCountByStatusAsync(string status);
}

/// <summary>
/// Servicio de Task
/// Encapsula la lógica de negocio para operaciones con tareas
/// Utiliza patrones creacionales: Factory Method, Builder, Prototype
/// </summary>
public class TaskService : ITaskService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly TaskFactoryProvider _taskFactory;

    public TaskService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _taskFactory = new TaskFactoryProvider();
    }

    public async System.Threading.Tasks.Task<TaskDto?> GetTaskByIdAsync(Guid id)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        return task != null ? MapToDto(task) : null;
    }

    public async System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetAllTasksAsync()
    {
        var tasks = await _unitOfWork.Tasks.GetAllAsync();
        return tasks.Select(MapToDto).ToList();
    }

    public async System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetTasksByProjectAsync(Guid projectId)
    {
        var tasks = await _unitOfWork.Tasks.GetTasksByProjectAsync(projectId);
        return tasks.Select(MapToDto).ToList();
    }

    public async System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetTasksByStatusAsync(string status)
    {
        if (!System.Enum.TryParse<TaskStatus>(status, true, out var parsedStatus))
        {
            throw new ArgumentException($"Invalid task status: {status}");
        }

        var tasks = await _unitOfWork.Tasks.GetTasksByStatusAsync(parsedStatus);
        return tasks.Select(MapToDto).ToList();
    }

    public async System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetTasksByAssigneeAsync(Guid userId)
    {
        var tasks = await _unitOfWork.Tasks.GetTasksByAssigneeAsync(userId);
        return tasks.Select(MapToDto).ToList();
    }

    public async System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetOverdueTasksAsync()
    {
        var tasks = await _unitOfWork.Tasks.GetOverdueTasksAsync();
        return tasks.Select(MapToDto).ToList();
    }

    public async System.Threading.Tasks.Task<TaskDto> CreateTaskAsync(CreateTaskDto createTaskDto)
    {
        if (!System.Enum.TryParse<TaskType>(createTaskDto.Type, true, out var taskType))
        {
            taskType = TaskType.TASK;
        }

        if (!System.Enum.TryParse<TaskPriority>(createTaskDto.Priority, true, out var priority))
        {
            priority = TaskPriority.MEDIUM;
        }

        var task = new TaskEntity
        {
            Title = createTaskDto.Title,
            Description = createTaskDto.Description,
            Type = taskType,
            Priority = priority,
            ProjectId = createTaskDto.ProjectId,
            AssignedToUserId = createTaskDto.AssignedToUserId,
            DueDate = createTaskDto.DueDate,
            EstimatedHours = createTaskDto.EstimatedHours,
            Tags = createTaskDto.Tags ?? new List<string>()
        };

        await _unitOfWork.Tasks.AddAsync(task);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(task);
    }

    public async System.Threading.Tasks.Task<TaskDto> CreateTaskByTypeAsync(string taskType, string title, string description, Guid projectId)
    {
        if (!System.Enum.TryParse<TaskType>(taskType, true, out var parsedType))
        {
            parsedType = TaskType.TASK;
        }

        var task = _taskFactory.CreateTask(parsedType, title, description, projectId);
        await _unitOfWork.Tasks.AddAsync(task);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(task);
    }

    public async System.Threading.Tasks.Task<TaskDto> UpdateTaskAsync(Guid id, UpdateTaskDto updateTaskDto)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        if (task == null)
        {
            throw new KeyNotFoundException($"Task with ID {id} not found");
        }

        if (!string.IsNullOrEmpty(updateTaskDto.Title))
            task.Title = updateTaskDto.Title;

        if (!string.IsNullOrEmpty(updateTaskDto.Description))
            task.Description = updateTaskDto.Description;

        if (!string.IsNullOrEmpty(updateTaskDto.Status) &&
            System.Enum.TryParse<TaskStatus>(updateTaskDto.Status, true, out var status))
            task.Status = status;

        if (!string.IsNullOrEmpty(updateTaskDto.Priority) &&
            System.Enum.TryParse<TaskPriority>(updateTaskDto.Priority, true, out var priority))
            task.Priority = priority;

        if (updateTaskDto.AssignedToUserId.HasValue)
            task.AssignedToUserId = updateTaskDto.AssignedToUserId;

        if (updateTaskDto.DueDate.HasValue)
            task.DueDate = updateTaskDto.DueDate;

        if (updateTaskDto.EstimatedHours.HasValue)
            task.EstimatedHours = updateTaskDto.EstimatedHours.Value;

        if (updateTaskDto.Tags != null)
            task.Tags = updateTaskDto.Tags;

        task.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Tasks.UpdateAsync(task);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(task);
    }

    public async System.Threading.Tasks.Task<bool> DeleteTaskAsync(Guid id)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        if (task == null)
        {
            return false;
        }

        await _unitOfWork.Tasks.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async System.Threading.Tasks.Task<TaskDto?> CloneTaskAsync(Guid taskId)
    {
        var originalTask = await _unitOfWork.Tasks.GetByIdAsync(taskId);
        if (originalTask == null)
        {
            return null;
        }

        var clonedTask = (TaskEntity)PrototypeManager.CloneTask(originalTask);
        await _unitOfWork.Tasks.AddAsync(clonedTask);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(clonedTask);
    }

    public async System.Threading.Tasks.Task<int> GetTaskCountByProjectAsync(Guid projectId)
    {
        return await _unitOfWork.Tasks.GetTaskCountByProjectAsync(projectId);
    }

    public async System.Threading.Tasks.Task<int> GetTaskCountByStatusAsync(string status)
    {
        if (!System.Enum.TryParse<TaskStatus>(status, true, out var parsedStatus))
        {
            throw new ArgumentException($"Invalid task status: {status}");
        }

        return await _unitOfWork.Tasks.GetTaskCountByStatusAsync(parsedStatus);
    }

    private TaskDto MapToDto(TaskEntity task)
    {
        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Type = task.Type.ToString(),
            Status = task.Status.ToString(),
            Priority = task.Priority.ToString(),
            ProjectId = task.ProjectId,
            AssignedToUserId = task.AssignedToUserId,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            DueDate = task.DueDate,
            EstimatedHours = task.EstimatedHours,
            Tags = task.Tags
        };
    }
}
