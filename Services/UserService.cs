using System.Text.Json;
using TaskFlow_API.DTOs;
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
    System.Threading.Tasks.Task<User?> GetUserByIdWithRoleAsync(Guid id);
    System.Threading.Tasks.Task<IEnumerable<User>> GetAllUsersAsync();
    System.Threading.Tasks.Task<User?> GetUserByEmailAsync(string email);
    System.Threading.Tasks.Task<User> CreateUserAsync(string email, string name, string passwordHash, string? avatarUrl = "");
    System.Threading.Tasks.Task<User> UpdateUserAsync(Guid id, string name, string? avatarUrl);
    System.Threading.Tasks.Task<bool> DeleteUserAsync(Guid id);
    System.Threading.Tasks.Task<bool> EmailExistsAsync(string email);
    System.Threading.Tasks.Task<IEnumerable<User>> GetUsersByProjectAsync(Guid projectId);
    System.Threading.Tasks.Task<int> GetProjectCountAsync(Guid userId);
    System.Threading.Tasks.Task<NotificationPreferencesDto> GetNotificationPreferencesAsync(Guid userId);
    System.Threading.Tasks.Task<NotificationPreferencesDto> UpdateNotificationPreferencesAsync(Guid userId, NotificationPreferencesDto dto);
    System.Threading.Tasks.Task<User> UpdateRoleAsync(Guid id, int appRoleId, bool? isActive);
}

/// <summary>
/// Servicio de User
/// Encapsula la l�gica de negocio para operaciones con usuarios
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

    /// <summary>
    /// Variante de GetUserById que carga la navegación AppRole — necesaria
    /// para mapear correctamente a UserDto.Role en endpoints públicos.
    /// </summary>
    public async System.Threading.Tasks.Task<User?> GetUserByIdWithRoleAsync(Guid id)
    {
        return await _unitOfWork.Users.GetByIdWithRoleAsync(id);
    }

    public async System.Threading.Tasks.Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _unitOfWork.Users.GetAllWithRoleAsync();
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

    // ----- RF-05.3: preferencias de notificación ----------------------------

    private static readonly string[] DefaultEventCodes =
        ["ASSIGNED", "DUE_OVERDUE", "COMMENT", "STATUS_CHANGE"];

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Devuelve las preferencias persistidas, completando con defaults los
    /// eventos que falten para que el cliente siempre reciba un mapa
    /// consistente.
    /// </summary>
    public async System.Threading.Tasks.Task<NotificationPreferencesDto> GetNotificationPreferencesAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User with ID {userId} not found");

        var result = new NotificationPreferencesDto();
        Dictionary<string, NotificationChannelPreferenceDto>? stored = null;

        if (!string.IsNullOrWhiteSpace(user.NotificationPreferences))
        {
            try
            {
                stored = JsonSerializer.Deserialize<Dictionary<string, NotificationChannelPreferenceDto>>(
                    user.NotificationPreferences, JsonOpts);
            }
            catch (JsonException)
            {
                // JSON corrupto en BD — lo tratamos como inexistente.
                stored = null;
            }
        }

        foreach (var code in DefaultEventCodes)
        {
            result.Preferences[code] = stored != null && stored.TryGetValue(code, out var v)
                ? v
                : new NotificationChannelPreferenceDto();
        }
        return result;
    }

    public async System.Threading.Tasks.Task<NotificationPreferencesDto> UpdateNotificationPreferencesAsync(Guid userId, NotificationPreferencesDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User with ID {userId} not found");

        // Normalizamos las claves a mayúsculas para evitar duplicados por casing.
        var sanitized = new Dictionary<string, NotificationChannelPreferenceDto>();
        foreach (var (key, value) in dto.Preferences ?? new())
        {
            if (string.IsNullOrWhiteSpace(key)) continue;
            sanitized[key.Trim().ToUpperInvariant()] = value ?? new NotificationChannelPreferenceDto();
        }

        user.NotificationPreferences = JsonSerializer.Serialize(sanitized);
        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return await GetNotificationPreferencesAsync(userId);
    }

    /// <summary>
    /// Cambia el rol (AppRole) del usuario indicado y, opcionalmente, su estado
    /// activo. Endpoint de uso exclusivo del Admin (la verificación se hace en
    /// el controller).
    /// </summary>
    public async System.Threading.Tasks.Task<User> UpdateRoleAsync(Guid id, int appRoleId, bool? isActive)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"User with ID {id} not found");

        // Validar que el AppRole existe en el catálogo — evita un FK violation
        // poco legible en SaveChangesAsync.
        var roles = await _unitOfWork.Catalog.GetAppRolesAsync();
        if (!roles.Any(r => r.Id == appRoleId))
            throw new ArgumentException($"AppRole con ID {appRoleId} no existe en el catálogo");

        user.AppRoleId = appRoleId;
        if (isActive.HasValue) user.IsActive = isActive.Value;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
        return user;
    }
}
