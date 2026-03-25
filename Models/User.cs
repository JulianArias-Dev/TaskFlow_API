namespace TaskFlow_API.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public int AppRoleId { get; set; }
        public AppRole AppRole { get; set; } = null!;

        public bool LightTheme { get; set; } = true;

        public string? AvatarUrl { get; set; }
        public bool AllowEmail { get; set; } = true;

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public List<Project> OwnedProjects { get; set; } = new(); // Proyectos que el usuario posee
        public List<TaskAssignment> Assignments { get; set; } = new(); // Tareas asignadas
        public List<Comment> Comments { get; set; } = new();
        public List<Notification> Notifications { get; set; } = new(); // Notificaciones del usuario

        // Relación Many-to-Many con Project (miembros de proyecto)
        public List<ProjectMember> ProjectMemberships { get; set; } = new();
    }
}

