using Microsoft.EntityFrameworkCore;
using TaskFlow_API.Data;
using TaskFlow_API.Models;

namespace TaskFlow_API.Repositories;

/// <summary>
/// Interfaz especializada para operaciones de Project
/// Extiende el repositorio genérico con métodos específicos de negocio
/// </summary>
public interface IProjectRepository : IRepository<Project>
{
    System.Threading.Tasks.Task<IEnumerable<Project>> GetProjectsByOwnerAsync(Guid ownerId);
    System.Threading.Tasks.Task<IEnumerable<Project>> GetProjectsByMemberAsync(Guid userId);
    System.Threading.Tasks.Task<Project?> GetProjectWithTasksAsync(Guid projectId);
    System.Threading.Tasks.Task<int> GetProjectCountByOwnerAsync(Guid ownerId);
    System.Threading.Tasks.Task AddMemberAsync(Guid projectId, Guid userId, UserRole role = UserRole.Developer);
    System.Threading.Tasks.Task RemoveMemberAsync(Guid projectId, Guid userId);
    System.Threading.Tasks.Task<bool> IsMemberAsync(Guid projectId, Guid userId);
    System.Threading.Tasks.Task<IEnumerable<Project>> GetAllProjectsWithTasksAsync();
}

/// <summary>
/// Repositorio especializado para Project
/// Implementa operaciones específicas del dominio de proyectos
/// </summary>
public class ProjectRepository : Repository<Project>, IProjectRepository
{
    public ProjectRepository(TaskFlowDbContext context) : base(context)
    {
    }

    public async System.Threading.Tasks.Task<IEnumerable<Project>> GetProjectsByOwnerAsync(Guid ownerId)
    {
        return await _dbSet
            .Where(p => p.OwnerId == ownerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<IEnumerable<Project>> GetProjectsByMemberAsync(Guid userId)
    {
        return await _dbSet
            .Include(p => p.Members)
            .Where(p => p.Members.Any(m => m.UserId == userId))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<Project?> GetProjectWithTasksAsync(Guid projectId)
    {
        return await _dbSet
			.Include(p => p.Members)
	        .Include(p => p.Boards)           // 1. Incluimos los Tableros
		        .ThenInclude(b => b.Columns)
				    .ThenInclude(p => p.Tasks)
			.FirstOrDefaultAsync(p => p.Id == projectId);
    }

    public async System.Threading.Tasks.Task<int> GetProjectCountByOwnerAsync(Guid ownerId)
    {
        return await _dbSet.CountAsync(p => p.OwnerId == ownerId);
    }

    public async System.Threading.Tasks.Task AddMemberAsync(Guid projectId, Guid userId, UserRole role = UserRole.Developer)
    {
        var project = await GetByIdAsync(projectId);
        if (project != null)
        {
            var projectMember = new ProjectMember
            {
                ProjectId = projectId,
                UserId = userId,
                Role = role
            };
            _context.ProjectMembers.Add(projectMember);
            await _context.SaveChangesAsync();
        }
    }

    public async System.Threading.Tasks.Task RemoveMemberAsync(Guid projectId, Guid userId)
    {
        var member = await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        if (member != null)
        {
            _context.ProjectMembers.Remove(member);
            await _context.SaveChangesAsync();
        }
    }

    public async System.Threading.Tasks.Task<bool> IsMemberAsync(Guid projectId, Guid userId)
    {
        return await _context.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
    }

    public async System.Threading.Tasks.Task<IEnumerable<Project>> GetAllProjectsWithTasksAsync()
    {
        return await _dbSet
            .Include(p => p.Tasks)
            .Include(p => p.Boards)
            //.Include(p => p.Boards.Select(b => b.Columns))
			.OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}
