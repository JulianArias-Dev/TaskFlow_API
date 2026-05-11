using System.ComponentModel.DataAnnotations;
using TaskFlow_API.Validations;

namespace TaskFlow_API.DTOs;

/// <summary>
/// DTO para crear un nuevo usuario
/// </summary>
public class CreateUserDto
{
	[Required(ErrorMessage = "El email es requerido")]
	[EmailAddress(ErrorMessage = "El formato del email es inválido")]
	public string Email { get; set; } = string.Empty;

	[Required(ErrorMessage = "El nombre es requerido")]
	[StringLength(255, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 255 caracteres")]
	public string Name { get; set; } = string.Empty;

	[Required(ErrorMessage = "La contraseña es requerida")]
	[StringLength(255, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
	[RegexAttribute(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
		ErrorMessage = "La contraseña debe contener mayúsculas, minúsculas, números y caracteres especiales")]
	public string Password { get; set; } = string.Empty;

	// Sin validación [Url]: el frontend genera data URLs (data:image/...) que
	// no superan la validación estándar de Url. Se permite cualquier string
	// hasta tener un endpoint dedicado a subir avatares (POST /Attachments/avatar).
	public string? AvatarUrl { get; set; }

	[Required(ErrorMessage = "El ID de rol es requerido")]
	[Range(1, int.MaxValue, ErrorMessage = "ID de rol inválido")]
	public int RoleId { get; set; } = 2; // AppRole: 1=Admin, 2=CommonUser
}

/// <summary>
/// DTO para actualizar un usuario existente
/// </summary>
public class UpdateUserDto
{
    [StringLength(255, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 255 caracteres")]
    public string? Name { get; set; }

    // Sin validación [Url]: aceptamos data URLs (data:image/...) hasta que
    // exista un endpoint dedicado para subir avatares.
    public string? AvatarUrl { get; set; }

    [RegexAttribute(@"^(Admin|Manager|Developer|Viewer)$", ErrorMessage = "Rol de usuario inválido")]
    public string? Role { get; set; }
}

/// <summary>
/// DTO para cambiar el rol (AppRole) de un usuario — uso exclusivo del admin.
/// </summary>
public class UpdateUserRoleDto
{
    [Required(ErrorMessage = "El ID del rol es requerido")]
    [Range(1, int.MaxValue, ErrorMessage = "ID de rol inválido")]
    public int AppRoleId { get; set; }

    /// <summary>Opcional — habilita/deshabilita el usuario en el mismo request.</summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// DTO para cambiar contraseña
/// </summary>
public class ChangePasswordDto
{
    [Required(ErrorMessage = "La contraseña actual es requerida")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es requerida")]
    [StringLength(255, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    [RegexAttribute(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", 
        ErrorMessage = "La contraseña debe contener mayúsculas, minúsculas, números y caracteres especiales")]
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// DTO para respuesta de usuario completo
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
    public int ProjectCount { get; set; } = 0;
    public int TaskCount { get; set; } = 0;
}

/// <summary>
/// DTO para respuesta simplificada de usuario
/// </summary>
public class UserSimpleDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// DTO para login
/// </summary>
public class LoginDto
{
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "El formato del email es inválido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// DTO para respuesta de login (con token)
/// </summary>
public class LoginResponseDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string ThemePreference { get; set; } = "Light";
    public bool NotifyByEmail { get; set; } = true;
	public DateTime ExpiresAt { get; set; }
    public DateTime LastConnection { get; set; }
    /// <summary>JSON crudo con las preferencias por evento (RF-05.3).</summary>
    public string? NotificationPreferences { get; set; }
}
