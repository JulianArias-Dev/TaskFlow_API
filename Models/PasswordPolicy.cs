using System.ComponentModel.DataAnnotations;

namespace TaskFlow_API.Models;

/// <summary>
/// Política global de contraseñas (configuración del sistema).
/// El diagrama ER omite la PK; aquí se asigna `PolicyId` por requisito de EF Core.
/// Pensado para una única fila activa (singleton lógico) — el seed inicializa la fila por defecto.
/// </summary>
public class PasswordPolicy
{
    public int PolicyId { get; set; }

    /// <summary>Longitud mínima permitida.</summary>
    [Range(1, 128)]
    public int MinLength { get; set; } = 8;

    /// <summary>Longitud máxima permitida.</summary>
    [Range(1, 256)]
    public int MaxLength { get; set; } = 64;

    /// <summary>Cantidad mínima de mayúsculas requeridas.</summary>
    [Range(0, 32)]
    public int MinUpperChars { get; set; } = 1;

    /// <summary>Cantidad mínima de caracteres especiales requeridos.</summary>
    [Range(0, 32)]
    public int MinSpecialChars { get; set; } = 1;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
