using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow_API.DTOs;
using TaskFlow_API.Models;
using TaskFlow_API.Services;

namespace TaskFlow_API.Controllers;

/// <summary>
/// Controller para gestionar operaciones de usuarios (Users)
/// Proporciona endpoints CRUD y operaciones de gesti�n de usuarios
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService,
        ILogger<UsersController> logger,
        INotificationService notificationService,
        IMapper mapper)
    {
        _userService = userService;
        _notificationService = notificationService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene un usuario por ID (devuelve UserDto con el rol resuelto).
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUserById(Guid id)
    {
        var user = await _userService.GetUserByIdWithRoleAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found", id);
            return NotFound(new { message = $"User with ID {id} not found" });
        }
        return Ok(_mapper.Map<UserDto>(user));
    }

    /// <summary>
    /// Obtiene todos los usuarios con su rol resuelto.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(_mapper.Map<IEnumerable<UserDto>>(users));
    }

    /// <summary>
    /// Obtiene un usuario por email.
    /// </summary>
    [HttpGet("email/{email}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUserByEmail(string email)
    {
        var user = await _userService.GetUserByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("User with email {Email} not found", email);
            return NotFound(new { message = $"User with email {email} not found" });
        }
        return Ok(_mapper.Map<UserDto>(user));
    }

    /// <summary>
    /// Obtiene usuarios que pertenecen a un proyecto específico.
    /// </summary>
    [HttpGet("project/{projectId}")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsersByProject(Guid projectId)
    {
        var users = await _userService.GetUsersByProjectAsync(projectId);
        return Ok(_mapper.Map<IEnumerable<UserDto>>(users));
    }

    /// <summary>
    /// Crea un nuevo usuario
    /// </summary>
    /// <param name="email">Email del usuario</param>
    /// <param name="name">Nombre del usuario</param>
    /// <param name="passwordHash">Hash de la contrase�a</param>
    /// <param name="avatar">Avatar/imagen del usuario (opcional)</param>
    /// <returns>Usuario creado</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<User>> CreateUser(
        [FromQuery] string email,
        [FromQuery] string name,
        [FromQuery] string passwordHash,
        [FromQuery] string? avatar = "")
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return BadRequest(new { message = "Email, name, and password hash are required" });
        }

        try
        {
            var createdUser = await _userService.CreateUserAsync(email, name, passwordHash, avatar);
            return CreatedAtAction(nameof(GetUserById), new { id = createdUser.Id }, createdUser);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("User creation failed: {Message}", ex.Message);
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza un usuario existente
    /// </summary>
    /// <param name="id">ID del usuario a actualizar</param>
    /// <param name="name">Nuevo nombre</param>
    /// <param name="avatar">Nuevo avatar (opcional)</param>
    /// <returns>Usuario actualizado</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<User>> UpdateUser(
        Guid id,
        [FromQuery] string name,
        [FromQuery] string? avatar = "")
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "Name is required" });
        }

        try
        {
            var updatedUser = await _userService.UpdateUserAsync(id, name, avatar);
            return Ok(updatedUser);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("User with ID {UserId} not found for update", id);
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cambia el rol (AppRole) de un usuario. Requiere rol Admin.
    /// Permite también activar/desactivar la cuenta en el mismo request.
    /// </summary>
    [HttpPut("{id}/role")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<User>> UpdateUserRole(Guid id, [FromBody] UpdateUserRoleDto dto)
    {
        if (dto == null) return BadRequest(new { message = "Payload requerido" });

        try
        {
            var updated = await _userService.UpdateRoleAsync(id, dto.AppRoleId, dto.IsActive);
            _logger.LogInformation("Admin actualizó el rol del usuario {UserId} a {RoleId}", id, dto.AppRoleId);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza el perfil de un usuario aceptando los datos en el body.
    /// Recomendado sobre el PUT clásico cuando se envía un AvatarUrl largo
    /// (p. ej. data URLs base64 generadas en el cliente).
    /// </summary>
    [HttpPut("{id}/profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<User>> UpdateProfile(Guid id, [FromBody] UpdateUserDto dto)
    {
        if (dto == null)
            return BadRequest(new { message = "Payload requerido" });

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "El nombre es requerido" });

        try
        {
            var updated = await _userService.UpdateUserAsync(id, dto.Name!, dto.AvatarUrl, dto.ThemePreference);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("User with ID {UserId} not found for profile update", id);
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Elimina un usuario
    /// </summary>
    /// <param name="id">ID del usuario a eliminar</param>
    /// <returns>Confirmaci�n de eliminaci�n</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteUser(Guid id)
    {
        var result = await _userService.DeleteUserAsync(id);
        if (!result)
        {
            _logger.LogWarning("User with ID {UserId} not found for deletion", id);
            return NotFound(new { message = $"User with ID {id} not found" });
        }
        return NoContent();
    }

    /// <summary>
    /// Verifica si un email existe
    /// </summary>
    /// <param name="email">Email a verificar</param>
    /// <returns>Booleano indicando si el email existe</returns>
    [HttpGet("exists/{email}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<bool>> EmailExists(string email)
    {
        var exists = await _userService.EmailExistsAsync(email);
        return Ok(new { exists });
    }

    /// <summary>
    /// Obtiene el conteo de proyectos de un usuario
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <returns>Cantidad de proyectos</returns>
    [HttpGet("{userId}/project-count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> GetProjectCount(Guid userId)
    {
        var count = await _userService.GetProjectCountAsync(userId);
        return Ok(new { count });
    }

	/// <summary>
	/// Obtiene las notificaciones de un usuario
	/// </summary>
	/// <returns></returns>
	[HttpGet("my-notifications")]
	[Authorize]
	public async Task<ActionResult<IEnumerable<NotificationDto>>> GetMyNotifications()
	{
		// Extraer el ID del usuario desde los Claims del Token
		var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

		var userId = Guid.Parse(userIdClaim);

		// Consulta directa (puedes moverla al service si tienes tiempo, si no, as� es m�s r�pido)
		var notifications = await _notificationService.GetUserNotificationsAsync(userId);

		return Ok(notifications);
	}

	/// <summary>
	///  Marca la notificación como leída
	/// </summary>
	[HttpPost("notifications/{id}/read")]
	[Authorize]
	public async Task<IActionResult> MarkAsRead(Guid id)
	{
		await _notificationService.MarkAsReadAsync(id);

		return NoContent();
	}

	/// <summary>
	/// Devuelve las preferencias de notificación del usuario autenticado.
	/// RF-05.3 — qué eventos y por qué canal.
	/// </summary>
	[HttpGet("me/notification-preferences")]
	[Authorize]
	[ProducesResponseType(typeof(ResponseDto<NotificationPreferencesDto>), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ResponseDto), StatusCodes.Status401Unauthorized)]
	public async Task<ActionResult<ResponseDto<NotificationPreferencesDto>>> GetMyNotificationPreferences()
	{
		var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
		if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

		var prefs = await _userService.GetNotificationPreferencesAsync(userId);
		return Ok(ResponseDto<NotificationPreferencesDto>.SuccessResponse(prefs, "Preferences retrieved"));
	}

	/// <summary>
	/// Actualiza las preferencias de notificación del usuario autenticado.
	/// </summary>
	[HttpPut("me/notification-preferences")]
	[Authorize]
	[ProducesResponseType(typeof(ResponseDto<NotificationPreferencesDto>), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ResponseDto), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(ResponseDto), StatusCodes.Status401Unauthorized)]
	public async Task<ActionResult<ResponseDto<NotificationPreferencesDto>>> UpdateMyNotificationPreferences(
		[FromBody] NotificationPreferencesDto dto)
	{
		var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
		if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

		try
		{
			var saved = await _userService.UpdateNotificationPreferencesAsync(userId, dto);
			_logger.LogInformation("User {UserId} updated notification preferences", userId);
			return Ok(ResponseDto<NotificationPreferencesDto>.SuccessResponse(saved, "Preferences updated"));
		}
		catch (KeyNotFoundException ex)
		{
			return NotFound(ResponseDto.NotFoundResponse(ex.Message));
		}
	}
}
