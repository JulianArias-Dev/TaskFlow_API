namespace TaskFlow_API.Models;

public class Column
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    
    // Foreign Key
    public Guid BoardId { get; set; }
    
    // WIP Limit (Work In Progress)
    public int? WipLimit { get; set; } // Null si no hay límite
    
    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int DisplayOrder { get; set; } = 0;
    public string Color { get; set; } = "#ecf0f1";

    // Navigation Properties
    public Board? Board { get; set; }
    public List<Task> Tasks { get; set; } = new();
}
