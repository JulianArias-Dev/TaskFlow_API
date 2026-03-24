namespace TaskFlow_API.Models;

public class Comment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Content { get; set; } = string.Empty;
    
    // Foreign Keys
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsEdited { get; set; } = false;

    // Navigation Properties
    public Task? Task { get; set; }
    public User? User { get; set; }
}
