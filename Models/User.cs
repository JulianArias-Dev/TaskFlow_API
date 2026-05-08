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

        public string? AvatarUrl { get; set; }

        // ----- Preferencias del usuario (alineadas al diagrama ER actualizado) -----

        /// <summary>
        /// Tema visual preferido. Se persiste como string vía HasConversion en
        /// el DbContext, de modo que el cliente Angular/React reciba "Light" /
        /// "Dark" / "System" listos para alimentar su Abstract Factory de UI.
        /// </summary>
        public ThemePreference ThemePreference { get; set; } = ThemePreference.Light;

        /// <summary>Si el usuario quiere recibir notificaciones por email.</summary>
        public bool NotifyByEmail { get; set; } = true;

        // ----- Metadata -----
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;

        // ----- Navegación -----
        public List<Project> OwnedProjects { get; set; } = new();
        public List<TaskAssignment> Assignments { get; set; } = new();
        public List<Comment> Comments { get; set; } = new();
        public List<Notification> Notifications { get; set; } = new();
        public List<ProjectMember> ProjectMemberships { get; set; } = new();
    }
}
