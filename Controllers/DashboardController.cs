using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow_API.DTOs;
using TaskFlow_API.Patterns.Facade;

namespace TaskFlow_API.Controllers;

/// <summary>
/// Controller para el Dashboard de proyectos (RF-08.1).
/// Toda la complejidad de combinar tareas + miembros + métricas vive en
/// `ProjectFacade`. El controller queda en una sola línea.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IProjectFacade _facade;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IProjectFacade facade, ILogger<DashboardController> logger)
    {
        _facade = facade;
        _logger = logger;
    }

    /// <summary>Devuelve el dashboard centralizado del proyecto.</summary>
    [HttpGet("project/{projectId}")]
    public async Task<ActionResult<ResponseDto<DashboardDto>>> GetProjectDashboard(Guid projectId)
    {
        try
        {
            var dashboard = await _facade.GetDashboardAsync(projectId);
            return Ok(ResponseDto<DashboardDto>.SuccessResponse(dashboard, "Dashboard generated"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Dashboard request failed: {Message}", ex.Message);
            return NotFound(ResponseDto.NotFoundResponse(ex.Message));
        }
    }
}
