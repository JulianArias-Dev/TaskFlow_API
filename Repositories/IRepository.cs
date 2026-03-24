using Microsoft.EntityFrameworkCore;
using TaskFlow_API.Data;

namespace TaskFlow_API.Repositories;

/// <summary>
/// Interfaz genérica del patrón Repository
/// Define las operaciones CRUD básicas y paginación para cualquier entidad
/// </summary>
public interface IRepository<TEntity> where TEntity : class
{
    System.Threading.Tasks.Task<TEntity?> GetByIdAsync(Guid id);
    System.Threading.Tasks.Task<IEnumerable<TEntity>> GetAllAsync();
    System.Threading.Tasks.Task<IEnumerable<TEntity>> FindAsync(Func<TEntity, bool> predicate);
    System.Threading.Tasks.Task<(List<TEntity> items, int totalCount)> GetPagedAsync(int pageNumber, int pageSize);
    System.Threading.Tasks.Task AddAsync(TEntity entity);
    System.Threading.Tasks.Task AddRangeAsync(IEnumerable<TEntity> entities);
    System.Threading.Tasks.Task UpdateAsync(TEntity entity);
    System.Threading.Tasks.Task DeleteAsync(Guid id);
    System.Threading.Tasks.Task DeleteAsync(TEntity entity);
    System.Threading.Tasks.Task DeleteRangeAsync(IEnumerable<TEntity> entities);
    System.Threading.Tasks.Task<bool> ExistsAsync(Guid id);
    System.Threading.Tasks.Task<int> CountAsync();
    System.Threading.Tasks.Task SaveChangesAsync();    
}

/// <summary>
/// Implementación genérica del patrón Repository
/// Proporciona operaciones CRUD reutilizables y paginación para todas las entidades
/// </summary>
public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly TaskFlowDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public Repository(TaskFlowDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public async System.Threading.Tasks.Task<TEntity?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async System.Threading.Tasks.Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async System.Threading.Tasks.Task<IEnumerable<TEntity>> FindAsync(Func<TEntity, bool> predicate)
    {
        return await System.Threading.Tasks.Task.FromResult(_dbSet.Where(predicate).ToList());
    }

	public async System.Threading.Tasks.Task<(List<TEntity> items, int totalCount)> GetPagedAsync(int pageNumber, int pageSize)
    {
        var totalCount = await _dbSet.CountAsync();
        var items = await _dbSet
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, totalCount);
    }

    public async System.Threading.Tasks.Task AddAsync(TEntity entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public async System.Threading.Tasks.Task AddRangeAsync(IEnumerable<TEntity> entities)
    {
        await _dbSet.AddRangeAsync(entities);
    }

    public async System.Threading.Tasks.Task UpdateAsync(TEntity entity)
    {
        _dbSet.Update(entity);
        await System.Threading.Tasks.Task.CompletedTask;
    }

    public async System.Threading.Tasks.Task DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
        }
    }

    public async System.Threading.Tasks.Task DeleteAsync(TEntity entity)
    {
        _dbSet.Remove(entity);
        await System.Threading.Tasks.Task.CompletedTask;
    }

    public async System.Threading.Tasks.Task DeleteRangeAsync(IEnumerable<TEntity> entities)
    {
        _dbSet.RemoveRange(entities);
        await System.Threading.Tasks.Task.CompletedTask;
    }

    public async System.Threading.Tasks.Task<bool> ExistsAsync(Guid id)
    {
        return await _dbSet.FindAsync(id) != null;
    }

    public async System.Threading.Tasks.Task<int> CountAsync()
    {
        return await _dbSet.CountAsync();
    }

    public async System.Threading.Tasks.Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
