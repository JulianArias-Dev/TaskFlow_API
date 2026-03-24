using System.ComponentModel.DataAnnotations;
using TaskFlow_API.Validations;

namespace TaskFlow_API.DTOs;

/// <summary>
/// DTO para crear una nueva tarea
/// </summary>
public class CreateTaskDto
{
    [Required(ErrorMessage = "El título es requerido")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "El título debe tener entre 3 y 255 caracteres")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "La descripción no puede exceder 2000 caracteres")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "El tipo de tarea es requerido")]
    [RegexAttribute(@"^(BUG|FEATURE|TASK|IMPROVEMENT|SUBTASK)$", ErrorMessage = "Tipo de tarea inválido")]
    public string Type { get; set; } = "TASK";

    [Required(ErrorMessage = "El ID del proyecto es requerido")]
    public Guid ProjectId { get; set; }

    [Required(ErrorMessage = "El ID de la columna es requerido")]
    public Guid ColumnId { get; set; }

    [RegexAttribute(@"^(LOW|MEDIUM|HIGH|CRITICAL)$", ErrorMessage = "Prioridad inválida")]
    public string Priority { get; set; } = "MEDIUM";

    public Guid? AssignedToUserId { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? DueDate { get; set; }

    [Range(0, 999, ErrorMessage = "Las horas estimadas no pueden ser negativas")]
    public int EstimatedHours { get; set; } = 0;

    public List<string> Tags { get; set; } = new();

    public Guid? ParentTaskId { get; set; }
}

/// <summary>
/// DTO para actualizar una tarea existente
/// </summary>
public class UpdateTaskDto
{
    [StringLength(255, MinimumLength = 3, ErrorMessage = "El título debe tener entre 3 y 255 caracteres")]
    public string? Title { get; set; }

    [StringLength(2000, ErrorMessage = "La descripción no puede exceder 2000 caracteres")]
    public string? Description { get; set; }

	[RegexAttribute(@"^(BUG|FEATURE|TASK|IMPROVEMENT|SUBTASK)$", ErrorMessage = "Tipo de tarea inválido")]
	public string? Type { get; set; }

    [RegexAttribute(@"^(TODO|IN_PROGRESS|IN_REVIEW|DONE|BLOCKED)$", ErrorMessage = "Estado inválido")]
    public string? Status { get; set; }

    [RegexAttribute(@"^(LOW|MEDIUM|HIGH|CRITICAL)$", ErrorMessage = "Prioridad inválida")]
    public string? Priority { get; set; }

	public List<Guid>? AssignedUserIds { get; set; }

	[DataType(DataType.DateTime)]
    public DateTime? DueDate { get; set; }

    [Range(0, 999, ErrorMessage = "Las horas estimadas no pueden ser negativas")]
    public int? EstimatedHours { get; set; }

    [Range(0, 999, ErrorMessage = "Las horas actuales no pueden ser negativas")]
    public int? ActualHours { get; set; }

    public List<string>? Tags { get; set; }
}

/// <summary>
/// DTO para respuesta de tarea
/// </summary>
public class TaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public Guid ColumnId { get; set; }
    public List<Guid> AssignedUserIds { get; set; } = new();
	public Guid? ParentTaskId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public int EstimatedHours { get; set; }
    public int ActualHours { get; set; }
    public List<string> Tags { get; set; } = new();
    public int SubTaskCount { get; set; } = 0;
    public int CommentCount { get; set; } = 0;
    public int FileCount { get; set; } = 0;
}

/// <summary>
/// DTO para respuesta simplificada de tarea
/// </summary>
public class TaskSimpleDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public List<Guid> AssignedUserIds { get; set; } = new();
    public DateTime? DueDate { get; set; }
}
