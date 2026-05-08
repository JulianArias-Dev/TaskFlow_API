using System.ComponentModel.DataAnnotations;

namespace TaskFlow_API.DTOs;

public class SavedFilterDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FilterCriteria { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSavedFilterDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string FilterCriteria { get; set; } = "{}";
}

public class UpdateSavedFilterDto
{
    [StringLength(100, MinimumLength = 1)]
    public string? Name { get; set; }
    public string? FilterCriteria { get; set; }
}
