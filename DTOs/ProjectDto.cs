using System.ComponentModel.DataAnnotations;
using TaskFlow_API.Validations;

namespace TaskFlow_API.DTOs;

/// <summary>
/// DTO para crear un nuevo proyecto
/// </summary>
public class CreateProjectDto
{
    [Required(ErrorMessage = "El nombre del proyecto es requerido")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 255 caracteres")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
    public string? Description { get; set; }

    [RegexAttribute("^#[0-9A-Fa-f]{6}$", ErrorMessage = "El color debe ser un código hexadecimal válido")]
    public string Color { get; set; } = "#3498db";

    [Required(ErrorMessage ="El proyecto debe tener una fecha de Inicio")]
    [DataType(DataType.DateTime)]
    public DateTime? StartDate { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? EndDate { get; set; }

    [RegexAttribute(@"^(Active|Archived|OnHold|Completed)$", ErrorMessage = "Estado de proyecto inválido")]
    public string Status { get; set; } = "Active";
}

/// <summary>
/// DTO para actualizar un proyecto existente
/// </summary>
public class UpdateProjectDto
{
    [StringLength(255, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 255 caracteres")]
    public string? Name { get; set; }

    [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
    public string? Description { get; set; }

    [RegexAttribute("^#[0-9A-Fa-f]{6}$", ErrorMessage = "El color debe ser un código hexadecimal válido")]
    public string? Color { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? StartDate { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? EndDate { get; set; }

    [RegexAttribute(@"^(Active|Archived|OnHold|Completed)$", ErrorMessage = "Estado de proyecto inválido")]
    public string? Status { get; set; }
}

/// <summary>
/// DTO para respuesta de proyecto completo
/// </summary>
public class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<Guid> MemberIds { get; set; } = new();
    public List<TaskDto> Tasks { get; set; } = new();
    public int TaskCount { get; set; } = 0;
    public int MemberCount { get; set; } = 0;
	public int BoardCount { get; set; } = 0;
	public List<BoardDto> Boards { get; set; } = new();
    
}

/// <summary>
/// DTO para respuesta simplificada de proyecto
/// </summary>
public class ProjectSimpleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public int TaskCount { get; set; }
    public int MemberCount { get; set; }
}
