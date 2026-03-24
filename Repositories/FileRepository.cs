using Microsoft.EntityFrameworkCore;
using TaskFlow_API.Data;
using FileEntity = TaskFlow_API.Models.File;

namespace TaskFlow_API.Repositories;

/// <summary>
/// Interfaz especializada para operaciones de File
/// </summary>
public interface IFileRepository : IRepository<FileEntity>
{
    System.Threading.Tasks.Task<IEnumerable<FileEntity>> GetFilesByTaskAsync(Guid taskId);
    System.Threading.Tasks.Task<IEnumerable<FileEntity>> GetFilesByUserAsync(Guid userId);
    System.Threading.Tasks.Task<long> GetTotalSizeByTaskAsync(Guid taskId);
    System.Threading.Tasks.Task<(List<FileEntity> items, int totalCount)> GetFilesPagedByTaskAsync(Guid taskId, int pageNumber, int pageSize);
}

/// <summary>
/// Repositorio especializado para File
/// </summary>
public class FileRepository : Repository<FileEntity>, IFileRepository
{
    public FileRepository(TaskFlowDbContext context) : base(context)
    {
    }

    public async System.Threading.Tasks.Task<IEnumerable<FileEntity>> GetFilesByTaskAsync(Guid taskId)
    {
        return await _dbSet
            .Include(f => f.UploadedBy)
            .Where(f => f.TaskId == taskId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<IEnumerable<FileEntity>> GetFilesByUserAsync(Guid userId)
    {
        return await _dbSet
            .Where(f => f.UploadedByUserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<long> GetTotalSizeByTaskAsync(Guid taskId)
    {
        return await _dbSet
            .Where(f => f.TaskId == taskId)
            .SumAsync(f => f.FileSize);
    }

    public async System.Threading.Tasks.Task<(List<FileEntity> items, int totalCount)> GetFilesPagedByTaskAsync(Guid taskId, int pageNumber, int pageSize)
    {
        var query = _dbSet
            .Include(f => f.UploadedBy)
            .Where(f => f.TaskId == taskId);
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, totalCount);
    }
}
