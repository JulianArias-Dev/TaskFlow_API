using System.ComponentModel.DataAnnotations;
using TaskFlow_API.Validations;

namespace TaskFlow_API.DTOs;

/// <summary>
/// DTO para agregar un miembro a un proyecto
/// </summary>
public class AddProjectMemberDto
{
    [Required(ErrorMessage = "El ID del usuario es requerido")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "El ID del proyecto es requerido")]
    public Guid ProjectId { get; set; }

    [RegexAttribute(@"^(Admin|Manager|Developer|Viewer)$", ErrorMessage = "Rol inválido")]
    public string Role { get; set; } = "Developer";
}

/// <summary>
/// DTO para actualizar el rol de un miembro
/// </summary>
public class UpdateProjectMemberRoleDto
{
    [Required(ErrorMessage = "El rol es requerido")]
    [RegexAttribute(@"^(Admin|Manager|Developer|Viewer)$", ErrorMessage = "Rol inválido")]
    public string Role { get; set; } = "Developer";
}

/// <summary>
/// DTO para respuesta de miembro del proyecto
/// </summary>
public class ProjectMemberDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public int AssignedTaskCount { get; set; } = 0;
}
