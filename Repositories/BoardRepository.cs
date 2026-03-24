using Microsoft.EntityFrameworkCore;
using TaskFlow_API.Data;
using TaskFlow_API.Models;

namespace TaskFlow_API.Repositories;

/// <summary>
/// Interfaz especializada para operaciones de Board
/// </summary>
public interface IBoardRepository : IRepository<Board>
{
    System.Threading.Tasks.Task<IEnumerable<Board>> GetBoardsByProjectAsync(Guid projectId);
    System.Threading.Tasks.Task<Board?> GetBoardWithColumnsAsync(Guid boardId);
    System.Threading.Tasks.Task<(List<Board> items, int totalCount)> GetBoardsPagedByProjectAsync(Guid projectId, int pageNumber, int pageSize);
}

/// <summary>
/// Repositorio especializado para Board
/// </summary>
public class BoardRepository : Repository<Board>, IBoardRepository
{
    public BoardRepository(TaskFlowDbContext context) : base(context)
    {
    }

    public async System.Threading.Tasks.Task<IEnumerable<Board>> GetBoardsByProjectAsync(Guid projectId)
    {
        return await _dbSet
            .Include(b => b.Columns)
                .ThenInclude(c => c.Tasks)
            .Where(b => b.ProjectId == projectId)
            .OrderBy(b => b.DisplayOrder)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<Board?> GetBoardWithColumnsAsync(Guid boardId)
    {
        return await _dbSet
            .Include(b => b.Columns)
            .FirstOrDefaultAsync(b => b.Id == boardId);
    }

    public async System.Threading.Tasks.Task<(List<Board> items, int totalCount)> GetBoardsPagedByProjectAsync(Guid projectId, int pageNumber, int pageSize)
    {
        var query = _dbSet
            .Include(b => b.Columns)
            .ThenInclude(c => c.Tasks)
            .Where(b => b.ProjectId == projectId);
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(b => b.DisplayOrder)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, totalCount);
    }
}
