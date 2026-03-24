using Microsoft.EntityFrameworkCore;
using TaskFlow_API.Data;
using TaskFlow_API.Models;

namespace TaskFlow_API.Repositories;

/// <summary>
/// Interfaz especializada para operaciones de TaskTag
/// </summary>
public interface ITaskTagRepository : IRepository<TaskTag>
{
    System.Threading.Tasks.Task<IEnumerable<TaskTag>> GetTagsByTaskAsync(Guid taskId);
    System.Threading.Tasks.Task<IEnumerable<TaskTag>> GetTasksByTagAsync(Guid tagId);
    System.Threading.Tasks.Task<TaskTag?> GetTaskTagAsync(Guid taskId, Guid tagId);
    System.Threading.Tasks.Task RemoveTaskTagAsync(Guid taskId, Guid tagId);
}

/// <summary>
/// Repositorio especializado para TaskTag (relación N:M)
/// </summary>
public class TaskTagRepository : Repository<TaskTag>, ITaskTagRepository
{
    public TaskTagRepository(TaskFlowDbContext context) : base(context)
    {
    }

    public async System.Threading.Tasks.Task<IEnumerable<TaskTag>> GetTagsByTaskAsync(Guid taskId)
    {
        return await _dbSet
            .Include(tt => tt.Tag)
            .Where(tt => tt.TaskId == taskId)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<IEnumerable<TaskTag>> GetTasksByTagAsync(Guid tagId)
    {
        return await _dbSet
            .Include(tt => tt.Task)
            .Where(tt => tt.TagId == tagId)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<TaskTag?> GetTaskTagAsync(Guid taskId, Guid tagId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(tt => tt.TaskId == taskId && tt.TagId == tagId);
    }

    public async System.Threading.Tasks.Task RemoveTaskTagAsync(Guid taskId, Guid tagId)
    {
        var taskTag = await GetTaskTagAsync(taskId, tagId);
        if (taskTag != null)
        {
            await DeleteAsync(taskTag);
            await SaveChangesAsync();
        }
    }
}
