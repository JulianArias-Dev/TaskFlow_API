using Microsoft.EntityFrameworkCore;
using TaskFlow_API.Data;
using TaskFlow_API.Models;

namespace TaskFlow_API.Repositories;

/// <summary>
/// Interfaz especializada para operaciones de ProjectMember
/// </summary>
public interface IProjectMemberRepository : IRepository<ProjectMember>
{
    System.Threading.Tasks.Task<IEnumerable<ProjectMember>> GetMembersByProjectAsync(Guid projectId);
    System.Threading.Tasks.Task<IEnumerable<ProjectMember>> GetProjectsByMemberAsync(Guid userId);
    System.Threading.Tasks.Task<ProjectMember?> GetProjectMemberAsync(Guid projectId, Guid userId);
    System.Threading.Tasks.Task<(List<ProjectMember> items, int totalCount)> GetMembersPagedByProjectAsync(Guid projectId, int pageNumber, int pageSize);
    System.Threading.Tasks.Task<int> GetMemberCountByProjectAsync(Guid projectId);
}

/// <summary>
/// Repositorio especializado para ProjectMember (relación N:M)
/// </summary>
public class ProjectMemberRepository : Repository<ProjectMember>, IProjectMemberRepository
{
    public ProjectMemberRepository(TaskFlowDbContext context) : base(context)
    {
    }

    public async System.Threading.Tasks.Task<IEnumerable<ProjectMember>> GetMembersByProjectAsync(Guid projectId)
    {
        return await _dbSet
            .Include(pm => pm.User)
            .Include(pm => pm.Project)
            .Where(pm => pm.ProjectId == projectId)
            .OrderBy(pm => pm.User!.Name)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<IEnumerable<ProjectMember>> GetProjectsByMemberAsync(Guid userId)
    {
        return await _dbSet
            .Include(pm => pm.Project)
            .Where(pm => pm.UserId == userId)
            .OrderByDescending(pm => pm.JoinedAt)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<ProjectMember?> GetProjectMemberAsync(Guid projectId, Guid userId)
    {
        return await _dbSet
            .Include(pm => pm.User)
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
    }

    public async System.Threading.Tasks.Task<(List<ProjectMember> items, int totalCount)> GetMembersPagedByProjectAsync(Guid projectId, int pageNumber, int pageSize)
    {
        var query = _dbSet
            .Include(pm => pm.User)
            .Where(pm => pm.ProjectId == projectId);
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(pm => pm.User!.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, totalCount);
    }

    public async System.Threading.Tasks.Task<int> GetMemberCountByProjectAsync(Guid projectId)
    {
        return await _dbSet.CountAsync(pm => pm.ProjectId == projectId);
    }
}
