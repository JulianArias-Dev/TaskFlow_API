using System.ComponentModel.DataAnnotations;
using TaskFlow_API.Validations;

namespace TaskFlow_API.DTOs;

/// <summary>
/// DTO para crear un nuevo usuario
/// </summary>
public class CreateUserDto
{
	[Required(ErrorMessage = "El email es requerido")]
	[EmailAddress(ErrorMessage = "El formato del email es inv�lido")]
	public string Email { get; set; } = string.Empty;

	[Required(ErrorMessage = "El nombre es requerido")]
	[StringLength(255, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 255 caracteres")]
	public string Name { get; set; } = string.Empty;

	[Required(ErrorMessage = "La contrase�a es requerida")]
	[StringLength(255, MinimumLength = 8, ErrorMessage = "La contrase�a debe tener al menos 8 caracteres")]
	[RegexAttribute(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
		ErrorMessage = "La contrase�a debe contener may�sculas, min�sculas, n�meros y caracteres especiales")]
	public string Password { get; set; } = string.Empty;

	[Url(ErrorMessage = "La URL del avatar es inv�lida")]
	public string? AvatarUrl { get; set; }

	[Required(ErrorMessage = "El ID de rol es requerido")]
	[Range(1, int.MaxValue, ErrorMessage = "ID de rol inv�lido")]
	public int RoleId { get; set; } = 3; // Suponiendo que 3 es 'Developer' en tu tabla
}

/// <summary>
/// DTO para actualizar un usuario existente
/// </summary>
public class UpdateUserDto
{
    [StringLength(255, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 255 caracteres")]
    public string? Name { get; set; }

    [Url(ErrorMessage = "La URL del avatar es inv�lida")]
    public string? AvatarUrl { get; set; }

    [RegexAttribute(@"^(Admin|Manager|Developer|Viewer)$", ErrorMessage = "Rol de usuario inv�lido")]
    public string? Role { get; set; }
}

/// <summary>
/// DTO para cambiar contrase�a
/// </summary>
public class ChangePasswordDto
{
    [Required(ErrorMessage = "La contrase�a actual es requerida")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contrase�a es requerida")]
    [StringLength(255, MinimumLength = 8, ErrorMessage = "La contrase�a debe tener al menos 8 caracteres")]
    [RegexAttribute(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", 
        ErrorMessage = "La contrase�a debe contener may�sculas, min�sculas, n�meros y caracteres especiales")]
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
    [EmailAddress(ErrorMessage = "El formato del email es inv�lido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contrase�a es requerida")]
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
}
