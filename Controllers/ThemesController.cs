using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow_API.Patterns.AbstractFactory;
using TaskFlow_API.Services;

namespace TaskFlow_API.Controllers;

/// <summary>
/// Controller para gestionar temas visuales (Themes)
/// Proporciona endpoints para obtener configuraciones de temas
/// Utiliza el patr�n Abstract Factory
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class ThemesController : ControllerBase
{
    private readonly IThemeService _themeService;
    private readonly ILogger<ThemesController> _logger;

    public ThemesController(IThemeService themeService, ILogger<ThemesController> logger)
    {
        _themeService = themeService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene la configuraci�n de un tema espec�fico
    /// Utiliza el patr�n Abstract Factory
    /// </summary>
    /// <param name="themeName">Nombre del tema (dark, light)</param>
    /// <returns>Configuraci�n del tema solicitado</returns>
    [HttpGet("{themeName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ThemeConfiguration>> GetTheme(string themeName)
    {
        if (string.IsNullOrWhiteSpace(themeName))
        {
            return BadRequest(new { message = "Theme name is required" });
        }

        try
        {
            var theme = await _themeService.GetThemeAsync(themeName);
            return Ok(theme);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to get theme: {Message}", ex.Message);
            return BadRequest(new { message = "Invalid theme name" });
        }
    }

    /// <summary>
    /// Obtiene todos los temas disponibles
    /// </summary>
    /// <returns>Lista de todas las configuraciones de temas</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ThemeConfiguration>>> GetAllThemes()
    {
        var themes = await _themeService.GetAllThemesAsync();
        return Ok(themes);
    }

    /// <summary>
    /// Obtiene informaci�n de temas disponibles
    /// </summary>
    /// <returns>Lista de nombres de temas disponibles</returns>
    [HttpGet("info/available")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> GetAvailableThemes()
    {
        return Ok(new
        {
            available = new[] { "dark", "light" },
            description = "Use theme names to fetch specific theme configurations"
        });
    }
}
