namespace TaskFlow_API.Models;

public enum UserRole
{
    Admin,
    Project_Manager,
    Creator,
    Developer,
    CommonUser
}

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Developer;
    public string? AvatarUrl { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public List<Project> OwnedProjects { get; set; } = new(); // Proyectos que el usuario posee
    public List<Task> AssignedTasks { get; set; } = new(); // Tareas asignadas
    public List<Comment> Comments { get; set; } = new();
    
    // Relación Many-to-Many con Project (miembros de proyecto)
    public List<ProjectMember> ProjectMemberships { get; set; } = new();
}

