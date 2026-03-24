namespace TaskFlow_API.Patterns.AbstractFactory;

/// <summary>
/// Clase que representa la configuración de un tema visual
/// </summary>
public class ThemeConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string PrimaryColor { get; set; } = string.Empty;
    public string SecondaryColor { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
    public string BorderColor { get; set; } = string.Empty;
    public string AccentColor { get; set; } = string.Empty;
}

/// <summary>
/// Interfaz para las factorías de componentes de tema
/// </summary>
public interface IThemeComponentFactory
{
    ThemeConfiguration CreateThemeConfiguration();
}

/// <summary>
/// Factoría concreta para crear el tema Dark
/// </summary>
public class DarkThemeFactory : IThemeComponentFactory
{
    public ThemeConfiguration CreateThemeConfiguration()
    {
        return new ThemeConfiguration
        {
            Name = "Dark",
            PrimaryColor = "#1e1e1e",
            SecondaryColor = "#2d2d2d",
            BackgroundColor = "#121212",
            TextColor = "#e0e0e0",
            BorderColor = "#404040",
            AccentColor = "#bb86fc"
        };
    }
}

/// <summary>
/// Factoría concreta para crear el tema Light
/// </summary>
public class LightThemeFactory : IThemeComponentFactory
{
    public ThemeConfiguration CreateThemeConfiguration()
    {
        return new ThemeConfiguration
        {
            Name = "Light",
            PrimaryColor = "#ffffff",
            SecondaryColor = "#f5f5f5",
            BackgroundColor = "#fafafa",
            TextColor = "#212121",
            BorderColor = "#e0e0e0",
            AccentColor = "#6200ea"
        };
    }
}

/// <summary>
/// Abstract Factory: Proporciona la interfaz para crear familias
/// de objetos relacionados (temas visuales)
/// </summary>
public interface IThemeFactory
{
    IThemeComponentFactory CreateThemeFactory();
}

/// <summary>
/// Implementación de Abstract Factory para tema Dark
/// </summary>
public class DarkThemeProductFactory : IThemeFactory
{
    public IThemeComponentFactory CreateThemeFactory()
    {
        return new DarkThemeFactory();
    }
}

/// <summary>
/// Implementación de Abstract Factory para tema Light
/// </summary>
public class LightThemeProductFactory : IThemeFactory
{
    public IThemeComponentFactory CreateThemeFactory()
    {
        return new LightThemeFactory();
    }
}

/// <summary>
/// Proveedor centralizado para obtener configuraciones de temas
/// </summary>
public class ThemeProvider
{
    public static ThemeConfiguration GetThemeConfiguration(string themeName)
    {
        IThemeFactory factory = themeName.ToLower() switch
        {
            "dark" => new DarkThemeProductFactory(),
            "light" => new LightThemeProductFactory(),
            _ => new LightThemeProductFactory()
        };

        var componentFactory = factory.CreateThemeFactory();
        return componentFactory.CreateThemeConfiguration();
    }

    public static List<ThemeConfiguration> GetAllThemes()
    {
        return new List<ThemeConfiguration>
        {
            GetThemeConfiguration("dark"),
            GetThemeConfiguration("light")
        };
    }
}
