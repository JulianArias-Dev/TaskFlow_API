namespace TaskFlow_API.Models
{
    public class Project : ICloneable
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Color { get; set; } = "#3498db";

        public int StatusId { get; set; }
        public ProjectStatus Status { get; set; } = null!;

        // Foreign Keys
        public Guid OwnerId { get; set; }

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Navigation Properties
        public User? Owner { get; set; }
        public List<Board> Boards { get; set; } = new();
        public List<Task> Tasks { get; set; } = new();
        public List<Tag> Tags { get; set; } = new();

        // Relación Many-to-Many con User (miembros del proyecto)
        public List<ProjectMember> Members { get; set; } = new();

        public object Clone()
        {
            return new Project
            {
                Id = Guid.NewGuid(),
                Name = this.Name,
                Description = this.Description,
                Color = this.Color,
                Status = this.Status,
                OwnerId = this.OwnerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                StartDate = this.StartDate,
                EndDate = this.EndDate,
                Boards = this.Boards.Select(b => new Board
                {
                    Id = Guid.NewGuid(),
                    Name = b.Name,
                    ProjectId = Guid.NewGuid() // Será actualizado al guardar
                }).ToList(),
                Tasks = this.Tasks.Select(t => (Task)t.Clone()).ToList(),
                Tags = new(),
                Members = new()
            };
        }
    }
}

