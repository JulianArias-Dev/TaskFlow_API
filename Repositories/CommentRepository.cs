using Microsoft.EntityFrameworkCore;
using TaskFlow_API.Data;
using TaskFlow_API.Models;

namespace TaskFlow_API.Repositories;

/// <summary>
/// Interfaz especializada para operaciones de Comment
/// </summary>
public interface ICommentRepository : IRepository<Comment>
{
    System.Threading.Tasks.Task<IEnumerable<Comment>> GetCommentsByTaskAsync(Guid taskId);
    System.Threading.Tasks.Task<IEnumerable<Comment>> GetCommentsByUserAsync(Guid userId);
    System.Threading.Tasks.Task<(List<Comment> items, int totalCount)> GetCommentsPagedByTaskAsync(Guid taskId, int pageNumber, int pageSize);
}

/// <summary>
/// Repositorio especializado para Comment
/// </summary>
public class CommentRepository : Repository<Comment>, ICommentRepository
{
    public CommentRepository(TaskFlowDbContext context) : base(context)
    {
    }

    public async System.Threading.Tasks.Task<IEnumerable<Comment>> GetCommentsByTaskAsync(Guid taskId)
    {
        return await _dbSet
            .Include(c => c.User)
            .Where(c => c.TaskId == taskId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<IEnumerable<Comment>> GetCommentsByUserAsync(Guid userId)
    {
        return await _dbSet
            .Include(c => c.Task)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<(List<Comment> items, int totalCount)> GetCommentsPagedByTaskAsync(Guid taskId, int pageNumber, int pageSize)
    {
        var query = _dbSet
            .Include(c => c.User)
            .Where(c => c.TaskId == taskId);
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, totalCount);
    }
}
