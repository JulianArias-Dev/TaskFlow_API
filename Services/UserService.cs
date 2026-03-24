using TaskFlow_API.Models;
using TaskFlow_API.Repositories;

namespace TaskFlow_API.Services;

/// <summary>
/// Interfaz del servicio de User
/// Define operaciones de negocio para usuarios
/// </summary>
public interface IUserService
{
    System.Threading.Tasks.Task<User?> GetUserByIdAsync(Guid id);
    System.Threading.Tasks.Task<IEnumerable<User>> GetAllUsersAsync();
    System.Threading.Tasks.Task<User?> GetUserByEmailAsync(string email);
    System.Threading.Tasks.Task<User> CreateUserAsync(string email, string name, string passwordHash, string? avatarUrl = "");
    System.Threading.Tasks.Task<User> UpdateUserAsync(Guid id, string name, string? avatarUrl);
    System.Threading.Tasks.Task<bool> DeleteUserAsync(Guid id);
    System.Threading.Tasks.Task<bool> EmailExistsAsync(string email);
    System.Threading.Tasks.Task<IEnumerable<User>> GetUsersByProjectAsync(Guid projectId);
    System.Threading.Tasks.Task<int> GetProjectCountAsync(Guid userId);
}

/// <summary>
/// Servicio de User
/// Encapsula la lógica de negocio para operaciones con usuarios
/// </summary>
public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async System.Threading.Tasks.Task<User?> GetUserByIdAsync(Guid id)
    {
        return await _unitOfWork.Users.GetByIdAsync(id);
    }

    public async System.Threading.Tasks.Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _unitOfWork.Users.GetAllAsync();
    }

    public async System.Threading.Tasks.Task<User?> GetUserByEmailAsync(string email)
    {
        return await _unitOfWork.Users.GetUserByEmailAsync(email);
    }

    public async System.Threading.Tasks.Task<User> CreateUserAsync(string email, string name, string passwordHash, string? avatarUrl = "")
    {
        if (await _unitOfWork.Users.EmailExistsAsync(email))
        {
            throw new InvalidOperationException($"User with email {email} already exists");
        }

        var user = new User
        {
            Email = email,
            Name = name,
            PasswordHash = passwordHash,
            AvatarUrl = avatarUrl ?? ""
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return user;
    }

    public async System.Threading.Tasks.Task<User> UpdateUserAsync(Guid id, string name, string? avatarUrl)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {id} not found");
        }

        user.Name = name;
        user.AvatarUrl = avatarUrl ?? "";
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return user;
    }

    public async System.Threading.Tasks.Task<bool> DeleteUserAsync(Guid id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
        {
            return false;
        }

        await _unitOfWork.Users.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async System.Threading.Tasks.Task<bool> EmailExistsAsync(string email)
    {
        return await _unitOfWork.Users.EmailExistsAsync(email);
    }

    public async System.Threading.Tasks.Task<IEnumerable<User>> GetUsersByProjectAsync(Guid projectId)
    {
        return await _unitOfWork.Users.GetUsersByProjectAsync(projectId);
    }

    public async System.Threading.Tasks.Task<int> GetProjectCountAsync(Guid userId)
    {
        return await _unitOfWork.Users.GetProjectCountAsync(userId);
    }
}
