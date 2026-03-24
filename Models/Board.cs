namespace TaskFlow_API.Models;

public class Board
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Foreign Key
    public Guid ProjectId { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int DisplayOrder { get; set; } = 0;

    // Navigation Properties
    public Project? Project { get; set; }
    public List<Column> Columns { get; set; } = new();
}
