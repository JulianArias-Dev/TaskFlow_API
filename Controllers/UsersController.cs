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
	private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger, INotificationService notificationService)
    {
        _userService = userService;
        _notificationService = notificationService;
		_logger = logger;
    }

    /// <summary>
    /// Obtiene un usuario por ID
    /// </summary>
    /// <param name="id">ID del usuario</param>
    /// <returns>Datos del usuario</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<User>> GetUserById(Guid id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found", id);
            return NotFound(new { message = $"User with ID {id} not found" });
        }
        return Ok(user);
    }

    /// <summary>
    /// Obtiene todos los usuarios
    /// </summary>
    /// <returns>Lista de todos los usuarios</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// Obtiene un usuario por email
    /// </summary>
    /// <param name="email">Email del usuario</param>
    /// <returns>Datos del usuario</returns>
    [HttpGet("email/{email}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<User>> GetUserByEmail(string email)
    {
        var user = await _userService.GetUserByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("User with email {Email} not found", email);
            return NotFound(new { message = $"User with email {email} not found" });
        }
        return Ok(user);
    }

    /// <summary>
    /// Obtiene usuarios que pertenecen a un proyecto espec�fico
    /// </summary>
    /// <param name="projectId">ID del proyecto</param>
    /// <returns>Lista de usuarios del proyecto</returns>
    [HttpGet("project/{projectId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<User>>> GetUsersByProject(Guid projectId)
    {
        var users = await _userService.GetUsersByProjectAsync(projectId);
        return Ok(users);
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
	///  Marca la notificaci�n como le�da
	/// </summary>
	[HttpPost("notifications/{id}/read")]
	[Authorize]
	public async Task<IActionResult> MarkAsRead(Guid id)
	{
		await _notificationService.MarkAsReadAsync(id);

		return NoContent();
	}
}
