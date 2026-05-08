namespace TaskFlow_API.Models;

/// <summary>
/// Preferencia de tema visual del usuario.
/// Se persiste como string en la BD (vía HasConversion&lt;string&gt;()) para que
/// los valores sean legibles en consultas y compatibles con el cliente
/// (React/Angular Abstract Factory consume directamente el string "Light" / "Dark" / "System").
/// </summary>
public enum ThemePreference
{
    /// <summary>Tema claro.</summary>
    Light = 0,

    /// <summary>Tema oscuro.</summary>
    Dark = 1,

    /// <summary>Sigue la preferencia del sistema operativo / navegador.</summary>
    System = 2
}
