using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow_API.Services;

namespace TaskFlow_API.Controllers;

/// <summary>
/// Controller para generación de reportes (RF-08.3).
/// El patrón Bridge desacopla la lógica de extracción de datos de la generación
/// del formato — añadir un nuevo formato es trivial.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Genera el reporte de un proyecto en el formato solicitado.
    /// </summary>
    /// <param name="projectId">ID del proyecto</param>
    /// <param name="format">"pdf" | "csv"</param>
    [HttpGet("project/{projectId}")]
    public async Task<IActionResult> GetProjectReport(Guid projectId, [FromQuery] string format = "pdf")
    {
        if (!Enum.TryParse<ReportFormatKind>(format, ignoreCase: true, out var kind))
            return BadRequest($"Format '{format}' not supported. Use 'pdf' or 'csv'.");

        try
        {
            var (bytes, contentType, extension) = await _reportService.GenerateProjectReportAsync(projectId, kind);
            var filename = $"project-{projectId}-{DateTime.UtcNow:yyyyMMddHHmm}.{extension}";
            return File(bytes, contentType, filename);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
