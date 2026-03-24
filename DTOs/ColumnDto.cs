using System.ComponentModel.DataAnnotations;
using TaskFlow_API.Validations;

namespace TaskFlow_API.DTOs;

/// <summary>
/// DTO para crear una nueva columna
/// </summary>
public class CreateColumnDto
{
    [Required(ErrorMessage = "El nombre de la columna es requerido")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 255 caracteres")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "El ID del board es requerido")]
    public Guid BoardId { get; set; }

    [Range(0, 999, ErrorMessage = "El WIP Limit debe estar entre 0 y 999")]
    public int? WipLimit { get; set; }

    [RegexAttribute("^#[0-9A-Fa-f]{6}$", ErrorMessage = "El color debe ser un código hexadecimal válido")]
    public string Color { get; set; } = "#ecf0f1";    
}

/// <summary>
/// DTO para actualizar una columna
/// </summary>
public class UpdateColumnDto
{
    [StringLength(255, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 255 caracteres")]
    public string? Name { get; set; }

    [Range(0, 999, ErrorMessage = "El WIP Limit debe estar entre 0 y 999")]
    public int? WipLimit { get; set; }

    [RegexAttribute("^#[0-9A-Fa-f]{6}$", ErrorMessage = "El color debe ser un código hexadecimal válido")]
    public string? Color { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "El orden de visualización no puede ser negativo")]
    public int? DisplayOrder { get; set; }
}

/// <summary>
/// DTO para respuesta de columna
/// </summary>
public class ColumnDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid BoardId { get; set; }
    public int? WipLimit { get; set; }
    public string Color { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int DisplayOrder { get; set; }
	public int TaskCount { get; set; } = 0;
	public List<TaskSimpleDto> Tasks { get; set; } = new();    
}
