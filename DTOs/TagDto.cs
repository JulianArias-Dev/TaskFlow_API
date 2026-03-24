using System.ComponentModel.DataAnnotations;
using TaskFlow_API.Validations;

namespace TaskFlow_API.DTOs;

/// <summary>
/// DTO para crear una etiqueta
/// </summary>
public class CreateTagDto
{
    [Required(ErrorMessage = "El nombre de la etiqueta es requerido")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 100 caracteres")]
    public string Name { get; set; } = string.Empty;

    [RegexAttribute("^#[0-9A-Fa-f]{6}$", ErrorMessage = "El color debe ser un código hexadecimal válido")]
    public string Color { get; set; } = "#3498db";

    [Required(ErrorMessage = "El ID del proyecto es requerido")]
    public Guid ProjectId { get; set; }
}

/// <summary>
/// DTO para actualizar una etiqueta
/// </summary>
public class UpdateTagDto
{
    [StringLength(100, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 100 caracteres")]
    public string? Name { get; set; }

    [RegexAttribute("^#[0-9A-Fa-f]{6}$", ErrorMessage = "El color debe ser un código hexadecimal válido")]
    public string? Color { get; set; }
}

/// <summary>
/// DTO para respuesta de etiqueta
/// </summary>
public class TagDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TaskCount { get; set; } = 0;
}
