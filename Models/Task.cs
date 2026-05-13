namespace TaskFlow_API.Models
{
    public class TaskAssignment
    {
        public Guid TaskId { get; set; }
        public Task? Task { get; set; }

        public Guid UserId { get; set; }
        public User? User { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }

    public class Task : ICloneable
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public int TypeId { get; set; }
        public TaskType Type { get; set; } = null!;

        public int StatusId { get; set; }
        public TaskStatus Status { get; set; } = null!;

        public int PriorityId { get; set; }
        public TaskPriority Priority { get; set; } = null!;

        // Foreign Keys
        public Guid ColumnId { get; set; }
        public Guid? ParentTaskId { get; set; } // Para tareas recursivas/subtareas

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }
        public int EstimatedHours { get; set; } = 0;
        public int ActualHours { get; set; } = 0;
        public List<string> Tags { get; set; } = new();

        // Navigation Properties
        public Column? Column { get; set; }

        // Recursividad para subtareas
        public Task? ParentTask { get; set; }
        public List<Task> SubTasks { get; set; } = new();

        // Relaciones N:M
        /// <summary>Relación N:M Task &lt;-&gt; Tag vía la tabla intermedia `TaskLabels`.</summary>
        public List<TaskLabel> TaskLabels { get; set; } = new();
        public List<TaskAssignment> Assignments { get; set; } = new();

        // One-to-Many
        public List<Comment> Comments { get; set; } = new();
        public List<File> Files { get; set; } = new();

        public object Clone()
        {
            return new Task
            {
                Id = Guid.NewGuid(),
                Title = $"{this.Title} (Copy)",
                Description = this.Description,
                Type = this.Type,
                Status = this.Status,
                Priority = this.Priority,
                ColumnId = this.ColumnId,
                Assignments = new List<TaskAssignment>(),
                ParentTaskId = null, // No heredar relaci�n padre en clonado
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DueDate = this.DueDate,
                EstimatedHours = this.EstimatedHours,
                ActualHours = 0,
                Tags = new List<string>(this.Tags),
                TaskLabels = new(),
                Comments = new(),
                Files = new(),
                SubTasks = new()
            };
        }
    }
}

