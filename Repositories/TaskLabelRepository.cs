using Microsoft.EntityFrameworkCore;
using TaskFlow_API.Data;
using TaskFlow_API.Models;

namespace TaskFlow_API.Repositories;

public interface ITaskLabelRepository
{
    System.Threading.Tasks.Task<IEnumerable<TaskLabel>> GetTagsByTaskAsync(Guid taskId);
    System.Threading.Tasks.Task<IEnumerable<TaskLabel>> GetTasksByTagAsync(Guid tagId);
    System.Threading.Tasks.Task<TaskLabel?> GetAsync(Guid taskId, Guid tagId);
    System.Threading.Tasks.Task AddAsync(TaskLabel entity);
    System.Threading.Tasks.Task RemoveAsync(Guid taskId, Guid tagId);
    System.Threading.Tasks.Task<int> CountByTagAsync(Guid tagId);
}

/// <summary>
/// Repositorio para la tabla intermedia `TaskLabels` (relación N:M Task ↔ Tag).
/// La PK es compuesta `(TaskId, TagId)`, por eso no se hereda de `Repository&lt;T&gt;`
/// que asume PK `Guid Id`.
/// </summary>
public class TaskLabelRepository : ITaskLabelRepository
{
    private readonly TaskFlowDbContext _context;
    private readonly DbSet<TaskLabel> _set;

    public TaskLabelRepository(TaskFlowDbContext context)
    {
        _context = context;
        _set = context.TaskLabels;
    }

    public async System.Threading.Tasks.Task<IEnumerable<TaskLabel>> GetTagsByTaskAsync(Guid taskId)
        => await _set.Include(tl => tl.Tag).Where(tl => tl.TaskId == taskId).ToListAsync();

    public async System.Threading.Tasks.Task<IEnumerable<TaskLabel>> GetTasksByTagAsync(Guid tagId)
        => await _set.Include(tl => tl.Task).Where(tl => tl.TagId == tagId).ToListAsync();

    public async System.Threading.Tasks.Task<TaskLabel?> GetAsync(Guid taskId, Guid tagId)
        => await _set.FirstOrDefaultAsync(tl => tl.TaskId == taskId && tl.TagId == tagId);

    public async System.Threading.Tasks.Task AddAsync(TaskLabel entity)
        => await _set.AddAsync(entity);

    public async System.Threading.Tasks.Task RemoveAsync(Guid taskId, Guid tagId)
    {
        var entity = await GetAsync(taskId, tagId);
        if (entity != null) _set.Remove(entity);
    }

    public async System.Threading.Tasks.Task<int> CountByTagAsync(Guid tagId)
        => await _set.CountAsync(tl => tl.TagId == tagId);
}
