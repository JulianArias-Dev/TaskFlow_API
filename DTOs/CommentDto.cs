using System.ComponentModel.DataAnnotations;

namespace TaskFlow_API.DTOs;

/// <summary>
/// DTO para crear un comentario
/// </summary>
public class CreateCommentDto
{
    [Required(ErrorMessage = "El contenido del comentario es requerido")]
    [StringLength(5000, MinimumLength = 1, ErrorMessage = "El comentario debe tener entre 1 y 5000 caracteres")]
    public string Content { get; set; } = string.Empty;

    [Required(ErrorMessage = "El ID de la tarea es requerido")]
    public Guid TaskId { get; set; }

    [Required(ErrorMessage = "El ID del usuario es requerido")]
    public Guid UserId { get; set; }
}

/// <summary>
/// DTO para actualizar un comentario
/// </summary>
public class UpdateCommentDto
{
    [Required(ErrorMessage = "El contenido del comentario es requerido")]
    [StringLength(5000, MinimumLength = 1, ErrorMessage = "El comentario debe tener entre 1 y 5000 caracteres")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// DTO para respuesta de comentario
/// </summary>
public class CommentDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsEdited { get; set; }
}
