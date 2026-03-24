using Microsoft.EntityFrameworkCore;
using TaskFlow_API.Data;
using TaskFlow_API.Models;
using TaskEntity = TaskFlow_API.Models.Task;
using TaskStatus = TaskFlow_API.Models.TaskStatus;

namespace TaskFlow_API.Repositories;

/// <summary>
/// Interfaz especializada para operaciones de Task
/// Extiende el repositorio genérico con métodos específicos de negocio
/// </summary>
public interface ITaskRepository : IRepository<TaskEntity>
{
    System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetTasksByProjectAsync(Guid projectId);
    System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetTasksByColumnAsync(Guid columnId);
    System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetTasksByStatusAsync(TaskStatus status);
    System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetTasksByAssigneeAsync(Guid userId);
    System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetTasksByPriorityAsync(Models.TaskPriority priority);
    System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetTasksByTypeAsync(Models.TaskType type);
    System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetOverdueTasksAsync();
    System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetSubTasksAsync(Guid parentTaskId);
    System.Threading.Tasks.Task<(List<TaskEntity> items, int totalCount)> GetTasksPagedByProjectAsync(Guid projectId, int pageNumber, int pageSize);
    System.Threading.Tasks.Task<(List<TaskEntity> items, int totalCount)> GetTasksPagedByStatusAsync(TaskStatus status, int pageNumber, int pageSize);
    System.Threading.Tasks.Task<int> GetTaskCountByProjectAsync(Guid projectId);
    System.Threading.Tasks.Task<int> GetTaskCountByStatusAsync(TaskStatus status);
    System.Threading.Tasks.Task<int> GetTaskCountByColumnAsync(Guid columnId);
    System.Threading.Tasks.Task<IEnumerable<TaskEntity>> SearchTasksAsync(string searchTerm, Guid projectId);
    System.Threading.Tasks.Task<TaskEntity?> GetTaskWithAssignmentsAsync(Guid id);
	System.Threading.Tasks.Task<TaskEntity?> GetByIdWithSubtasksAsync(Guid id);
}

/// <summary>
/// Repositorio especializado para Task
/// Implementa operaciones específicas del dominio de tareas
/// </summary>
public class TaskRepository : Repository<TaskEntity>, ITaskRepository
{
    public TaskRepository(TaskFlowDbContext context) : base(context)
    {
    }

	// En tu repositorio de tareas (TaskRepository):
	public async Task<TaskEntity?> GetTaskWithAssignmentsAsync(Guid id)
	{
		return await _dbSet
			.Include(t => t.Assignments) 
			.FirstOrDefaultAsync(t => t.Id == id);
	}

	public async System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetTasksByProjectAsync(Guid projectId)
    {
        return await _dbSet
            .Include(t => t.AssignedTo)
            .Include(t => t.Column)
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.Priority)
            .ToListAsync();
    }

	public async System.Threading.Tasks.Task<TaskEntity?> GetByIdWithSubtasksAsync(Guid id)
	{
		return await _dbSet
			.Include(t => t.SubTasks)
			.FirstOrDefaultAsync(t => t.Id == id);
	}

	public async System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetTasksByColumnAsync(Guid columnId)
    {
        return await _dbSet
            .Include(t => t.AssignedTo)
            .Where(t => t.ColumnId == columnId)
            .OrderBy(t => t.Priority)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetTasksByStatusAsync(TaskStatus status)
    {
        return await _dbSet
            .Include(t => t.AssignedTo)
            .Where(t => t.Status == status)
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetTasksByAssigneeAsync(Guid userId)
    {
        return await _dbSet
            .Include(t => t.Column)
			.Where(t => t.Assignments.Any(a => a.UserId == userId))
			.OrderBy(t => t.DueDate)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetTasksByPriorityAsync(Models.TaskPriority priority)
    {
        return await _dbSet
            .Include(t => t.AssignedTo)
            .Where(t => t.Priority == priority)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetTasksByTypeAsync(Models.TaskType type)
    {
        return await _dbSet
            .Include(t => t.AssignedTo)
            .Where(t => t.Type == type)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetOverdueTasksAsync()
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Include(t => t.AssignedTo)
            .Where(t => t.DueDate < now && t.Status != TaskStatus.DONE)
            .OrderBy(t => t.DueDate)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<IEnumerable<TaskEntity>> GetSubTasksAsync(Guid parentTaskId)
    {
        return await _dbSet
            .Where(t => t.ParentTaskId == parentTaskId)
            .OrderBy(t => t.Priority)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<(List<TaskEntity> items, int totalCount)> GetTasksPagedByProjectAsync(Guid projectId, int pageNumber, int pageSize)
    {
        var query = _dbSet
            .Include(t => t.AssignedTo)
            .Include(t => t.Column)
            .Where(t => t.ProjectId == projectId);
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(t => t.Priority)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, totalCount);
    }

    public async System.Threading.Tasks.Task<(List<TaskEntity> items, int totalCount)> GetTasksPagedByStatusAsync(TaskStatus status, int pageNumber, int pageSize)
    {
        var query = _dbSet
            .Include(t => t.AssignedTo)
            .Where(t => t.Status == status);
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.UpdatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, totalCount);
    }

    public async System.Threading.Tasks.Task<int> GetTaskCountByProjectAsync(Guid projectId)
    {
        return await _dbSet.CountAsync(t => t.ProjectId == projectId);
    }

    public async System.Threading.Tasks.Task<int> GetTaskCountByStatusAsync(TaskStatus status)
    {
        return await _dbSet.CountAsync(t => t.Status == status);
    }

    public async System.Threading.Tasks.Task<int> GetTaskCountByColumnAsync(Guid columnId)
    {
        return await _dbSet.CountAsync(t => t.ColumnId == columnId);
    }

    public async System.Threading.Tasks.Task<IEnumerable<TaskEntity>> SearchTasksAsync(string searchTerm, Guid projectId)
    {
        var lowerSearchTerm = searchTerm.ToLower();
        return await _dbSet
            .Include(t => t.AssignedTo)
            .Where(t => t.ProjectId == projectId && (
                t.Title.ToLower().Contains(lowerSearchTerm) ||
                t.Description.ToLower().Contains(lowerSearchTerm)))
            .OrderBy(t => t.Priority)
            .ToListAsync();
    }
}
