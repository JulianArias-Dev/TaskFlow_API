using Microsoft.EntityFrameworkCore;
using TaskFlow_API.Data;
using TaskFlow_API.Models;

namespace TaskFlow_API.Repositories;

/// <summary>
/// Interfaz especializada para operaciones de User
/// Extiende el repositorio genérico con métodos específicos de negocio
/// </summary>
public interface IUserRepository : IRepository<User>
{
    System.Threading.Tasks.Task<User?> GetUserByEmailAsync(string email);
    System.Threading.Tasks.Task<IEnumerable<User>> GetUsersByProjectAsync(Guid projectId);
    System.Threading.Tasks.Task<bool> EmailExistsAsync(string email);
    System.Threading.Tasks.Task<int> GetProjectCountAsync(Guid userId);
}

/// <summary>
/// Repositorio especializado para User
/// Implementa operaciones específicas del dominio de usuarios
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(TaskFlowDbContext context) : base(context)
    {
    }

    public async System.Threading.Tasks.Task<User?> GetUserByEmailAsync(string email)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async System.Threading.Tasks.Task<IEnumerable<User>> GetUsersByProjectAsync(Guid projectId)
    {
        return await _dbSet
            .Include(u => u.ProjectMemberships)
            .Where(u => u.ProjectMemberships.Any(pm => pm.ProjectId == projectId))
            .OrderBy(u => u.Name)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<bool> EmailExistsAsync(string email)
    {
        return await _dbSet.AnyAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async System.Threading.Tasks.Task<int> GetProjectCountAsync(Guid userId)
    {
        var user = await GetByIdAsync(userId);
        return user?.ProjectMemberships.Count ?? 0;
    }
}
