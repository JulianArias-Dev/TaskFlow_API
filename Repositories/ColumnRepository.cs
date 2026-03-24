using Microsoft.EntityFrameworkCore;
using TaskFlow_API.Data;
using TaskFlow_API.Models;

namespace TaskFlow_API.Repositories;

/// <summary>
/// Interfaz especializada para operaciones de Column
/// </summary>
public interface IColumnRepository : IRepository<Column>
{
    System.Threading.Tasks.Task<IEnumerable<Column>> GetColumnsByBoardAsync(Guid boardId);
    System.Threading.Tasks.Task<Column?> GetColumnWithTasksAsync(Guid columnId);
    System.Threading.Tasks.Task<int> GetTaskCountByColumnAsync(Guid columnId);
    System.Threading.Tasks.Task<(List<Column> items, int totalCount)> GetColumnsPagedByBoardAsync(Guid boardId, int pageNumber, int pageSize);
}

/// <summary>
/// Repositorio especializado para Column
/// </summary>
public class ColumnRepository : Repository<Column>, IColumnRepository
{
    public ColumnRepository(TaskFlowDbContext context) : base(context)
    {
    }

    public async System.Threading.Tasks.Task<IEnumerable<Column>> GetColumnsByBoardAsync(Guid boardId)
    {
        return await _dbSet
            .Where(c => c.BoardId == boardId)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<Column?> GetColumnWithTasksAsync(Guid columnId)
    {
        return await _dbSet
            .Include(c => c.Tasks)
            .FirstOrDefaultAsync(c => c.Id == columnId);
    }

    public async System.Threading.Tasks.Task<int> GetTaskCountByColumnAsync(Guid columnId)
    {
        return await _context.Tasks
            .CountAsync(t => t.ColumnId == columnId);
    }

    public async System.Threading.Tasks.Task<(List<Column> items, int totalCount)> GetColumnsPagedByBoardAsync(Guid boardId, int pageNumber, int pageSize)
    {
        var query = _dbSet.Where(c => c.BoardId == boardId);
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.DisplayOrder)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, totalCount);
    }
}
