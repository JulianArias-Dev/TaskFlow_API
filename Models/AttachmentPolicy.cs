using System.ComponentModel.DataAnnotations;

namespace TaskFlow_API.Models;

/// <summary>
/// Política global para adjuntos (configuración del sistema).
/// El diagrama ER omite la PK; aquí se asigna `PolicyId` por requisito de EF Core.
/// </summary>
public class AttachmentPolicy
{
    public int PolicyId { get; set; }

    /// <summary>Tamaño máximo permitido por archivo, en MB.</summary>
    [Range(1, 1024)]
    public int MaxSizeMB { get; set; } = 10;

    /// <summary>Cantidad máxima de archivos por tarea.</summary>
    [Range(1, 1000)]
    public int MaxFilesByTask { get; set; } = 20;

    /// <summary>
    /// Lista de extensiones permitidas separadas por coma — ej. "pdf,png,jpg,docx".
    /// Se persiste como una columna nvarchar simple para evitar tablas accesorias.
    /// </summary>
    [MaxLength(500)]
    public string AllowedFormats { get; set; } = "pdf,png,jpg,jpeg,gif,docx,xlsx,pptx,txt,zip";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Helper de lectura — convierte `AllowedFormats` a lista de extensiones limpias.</summary>
    public IReadOnlyList<string> GetAllowedFormatList() =>
        AllowedFormats
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(f => f.ToLowerInvariant().TrimStart('.'))
            .ToList();
}
