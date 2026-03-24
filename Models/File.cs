namespace TaskFlow_API.Models;

public class File
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string MimeType { get; set; } = "application/octet-stream";
    public long FileSize { get; set; } // en bytes
    
    // Foreign Key
    public Guid TaskId { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UploadedByUserId { get; set; }

    // Navigation Properties
    public Task? Task { get; set; }
    public User? UploadedBy { get; set; }
}
