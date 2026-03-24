using Microsoft.EntityFrameworkCore;
using TaskFlow_API.Data;
using TaskFlow_API.Models;

namespace TaskFlow_API.Repositories;

/// <summary>
/// Interfaz especializada para operaciones de AuditLog
/// </summary>
public interface IAuditLogRepository : IRepository<AuditLog>
{
    System.Threading.Tasks.Task<IEnumerable<AuditLog>> GetLogsByEntityAsync(string entityType, Guid entityId);
    System.Threading.Tasks.Task<IEnumerable<AuditLog>> GetLogsByUserAsync(Guid userId);
    System.Threading.Tasks.Task<IEnumerable<AuditLog>> GetLogsByActionAsync(string action);
    System.Threading.Tasks.Task<IEnumerable<AuditLog>> GetLogsInDateRangeAsync(DateTime startDate, DateTime endDate);
    System.Threading.Tasks.Task<(List<AuditLog> items, int totalCount)> GetLogsPagedAsync(
        string? entityType = null, 
        Guid? entityId = null, 
        Guid? userId = null, 
        string? action = null,
        int pageNumber = 1, 
        int pageSize = 50);
}

/// <summary>
/// Repositorio especializado para AuditLog
/// </summary>
public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(TaskFlowDbContext context) : base(context)
    {
    }

    public async System.Threading.Tasks.Task<IEnumerable<AuditLog>> GetLogsByEntityAsync(string entityType, Guid entityId)
    {
        return await _dbSet
            .Where(al => al.EntityType == entityType && al.EntityId == entityId)
            .OrderByDescending(al => al.CreatedAt)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<IEnumerable<AuditLog>> GetLogsByUserAsync(Guid userId)
    {
        return await _dbSet
            .Where(al => al.UserId == userId)
            .OrderByDescending(al => al.CreatedAt)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<IEnumerable<AuditLog>> GetLogsByActionAsync(string action)
    {
        return await _dbSet
            .Where(al => al.Action == action)
            .OrderByDescending(al => al.CreatedAt)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<IEnumerable<AuditLog>> GetLogsInDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(al => al.CreatedAt >= startDate && al.CreatedAt <= endDate)
            .OrderByDescending(al => al.CreatedAt)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<(List<AuditLog> items, int totalCount)> GetLogsPagedAsync(
        string? entityType = null, 
        Guid? entityId = null, 
        Guid? userId = null, 
        string? action = null,
        int pageNumber = 1, 
        int pageSize = 50)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(al => al.EntityType == entityType);

        if (entityId != null && entityId != Guid.Empty)
            query = query.Where(al => al.EntityId == entityId);

        if (userId != null && userId != Guid.Empty)
            query = query.Where(al => al.UserId == userId);

        if (!string.IsNullOrEmpty(action))
            query = query.Where(al => al.Action == action);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(al => al.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
