namespace TaskFlow_API.Models;

/// <summary>
/// Entidad de unión para la relación Many-to-Many entre Task y Tag
/// </summary>
public class TaskTag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // Foreign Keys
    public Guid TaskId { get; set; }
    public Guid TagId { get; set; }

    // Navigation Properties
    public Task? Task { get; set; }
    public Tag? Tag { get; set; }
}
