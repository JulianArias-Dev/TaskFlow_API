using System.ComponentModel.DataAnnotations;
using TaskFlow_API.Validations;

namespace TaskFlow_API.DTOs;

/// <summary>
/// DTO para crear un nuevo board
/// </summary>
public class CreateBoardDto
{
    [Required(ErrorMessage = "El nombre del board es requerido")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 255 caracteres")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "El ID del proyecto es requerido")]
    public Guid ProjectId { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "El orden de visualización no puede ser negativo")]
    public int DisplayOrder { get; set; } = 0;
}

/// <summary>
/// DTO para actualizar un board
/// </summary>
public class UpdateBoardDto
{
    [StringLength(255, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 255 caracteres")]
    public string? Name { get; set; }

    [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
    public string? Description { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "El orden de visualización no puede ser negativo")]
    public int? DisplayOrder { get; set; }
}

/// <summary>
/// DTO para respuesta de board
/// </summary>
public class BoardDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int DisplayOrder { get; set; }
	public int ColumnCount { get; set; } = 0;
	public List<ColumnDto> Columns { get; set; } = new();    
}
