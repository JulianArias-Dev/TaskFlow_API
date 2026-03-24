using System.ComponentModel.DataAnnotations;

namespace TaskFlow_API.DTOs;

/// <summary>
/// DTO para subir un archivo
/// </summary>
public class UploadFileDto
{
    [Required(ErrorMessage = "El archivo es requerido")]
    public IFormFile File { get; set; } = null!;

    [Required(ErrorMessage = "El ID de la tarea es requerido")]
    public Guid TaskId { get; set; }

    [Required(ErrorMessage = "El ID del usuario es requerido")]
    public Guid UserId { get; set; }
}

/// <summary>
/// DTO para respuesta de archivo
/// </summary>
public class FileDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public Guid TaskId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public string? UploadedByUserName { get; set; }
}
