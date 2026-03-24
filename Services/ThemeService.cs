using TaskFlow_API.Patterns.AbstractFactory;

namespace TaskFlow_API.Services;

/// <summary>
/// Interfaz del servicio de Themes
/// Define operaciones para gestión de temas visuales
/// </summary>
public interface IThemeService
{
    System.Threading.Tasks.Task<ThemeConfiguration> GetThemeAsync(string themeName);
    System.Threading.Tasks.Task<List<ThemeConfiguration>> GetAllThemesAsync();
}

/// <summary>
/// Servicio de Themes
/// Encapsula la lógica de obtención de configuraciones de temas
/// Utiliza el patrón Abstract Factory
/// </summary>
public class ThemeService : IThemeService
{
    public async System.Threading.Tasks.Task<ThemeConfiguration> GetThemeAsync(string themeName)
    {
        return await System.Threading.Tasks.Task.FromResult(ThemeProvider.GetThemeConfiguration(themeName));
    }

    public async System.Threading.Tasks.Task<List<ThemeConfiguration>> GetAllThemesAsync()
    {
        return await System.Threading.Tasks.Task.FromResult(ThemeProvider.GetAllThemes());
    }
}
