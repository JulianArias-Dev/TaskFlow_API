using Microsoft.EntityFrameworkCore;
using TaskFlow_API.Data;
using TaskFlow_API.Models;

namespace TaskFlow_API.Repositories;

/// <summary>
/// Interfaz especializada para operaciones de Tag
/// </summary>
public interface ITagRepository : IRepository<Tag>
{
    System.Threading.Tasks.Task<IEnumerable<Tag>> GetTagsByProjectAsync(Guid projectId);
    System.Threading.Tasks.Task<Tag?> GetTagByNameAsync(string name, Guid projectId);
    System.Threading.Tasks.Task<IEnumerable<Tag>> SearchTagsAsync(string searchTerm, Guid projectId);
    System.Threading.Tasks.Task<int> GetTaskCountByTagAsync(Guid tagId);
    System.Threading.Tasks.Task<(List<Tag> items, int totalCount)> GetTagsPagedByProjectAsync(Guid projectId, int pageNumber, int pageSize);
}

/// <summary>
/// Repositorio especializado para Tag
/// </summary>
public class TagRepository : Repository<Tag>, ITagRepository
{
    public TagRepository(TaskFlowDbContext context) : base(context)
    {
    }

    public async System.Threading.Tasks.Task<IEnumerable<Tag>> GetTagsByProjectAsync(Guid projectId)
    {
        return await _dbSet
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<Tag?> GetTagByNameAsync(string name, Guid projectId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower() && t.ProjectId == projectId);
    }

    public async System.Threading.Tasks.Task<IEnumerable<Tag>> SearchTagsAsync(string searchTerm, Guid projectId)
    {
        return await _dbSet
            .Where(t => t.ProjectId == projectId && t.Name.ToLower().Contains(searchTerm.ToLower()))
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<int> GetTaskCountByTagAsync(Guid tagId)
    {
        return await _context.TaskTags
            .CountAsync(tt => tt.TagId == tagId);
    }

    public async System.Threading.Tasks.Task<(List<Tag> items, int totalCount)> GetTagsPagedByProjectAsync(Guid projectId, int pageNumber, int pageSize)
    {
        var query = _dbSet.Where(t => t.ProjectId == projectId);
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(t => t.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, totalCount);
    }
}
