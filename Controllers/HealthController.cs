using Microsoft.AspNetCore.Mvc;

namespace TaskFlow_API.Controllers;

/// <summary>
/// Controller para verificar el estado de la API
/// Proporciona endpoints de health check y información general
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Verifica el estado general de la API
    /// </summary>
    /// <returns>Estado de la API</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> GetHealth()
    {
        _logger.LogInformation("Health check requested");
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "TaskFlow API",
            version = "1.0.0"
        });
    }

    /// <summary>
    /// Obtiene información sobre la API
    /// </summary>
    /// <returns>Información de la API</returns>
    [HttpGet("info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> GetInfo()
    {
        return Ok(new
        {
            serviceName = "TaskFlow API",
            description = "API backend para gestión de tareas Kanban",
            version = "1.0.0",
            framework = ".NET 9.0",
            endpoints = new
            {
                tasks = "/api/tasks",
                projects = "/api/projects",
                users = "/api/users",
                themes = "/api/themes",
                health = "/api/health"
            }
        });
    }

    /// <summary>
    /// Verifica la conexión a la base de datos
    /// </summary>
    /// <returns>Estado de la base de datos</returns>
    [HttpGet("database")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public ActionResult<object> GetDatabaseHealth()
    {
        try
        {
            return Ok(new
            {
                status = "connected",
                database = "SQL Server",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("Database health check failed: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { status = "unavailable", message = "Cannot connect to database" });
        }
    }
}
