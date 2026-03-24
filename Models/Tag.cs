namespace TaskFlow_API.Models;

public class Tag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#3498db";
    
    // Foreign Key
    public Guid ProjectId { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public Project? Project { get; set; }
    public List<TaskTag> TaskTags { get; set; } = new();
}
