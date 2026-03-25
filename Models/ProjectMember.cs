namespace TaskFlow_API.Models;

/// <summary>
/// Entidad de unión para la relación Many-to-Many entre User y Project
/// Con información adicional sobre el rol del usuario en el proyecto
/// </summary>
public class ProjectMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // Foreign Keys
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }

	// Información adicional
	public int ProjectRoleId { get; set; }
	public ProjectRole ProjectRole { get; set; } = null!;

	public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public Project? Project { get; set; }
    public User? User { get; set; }
}
