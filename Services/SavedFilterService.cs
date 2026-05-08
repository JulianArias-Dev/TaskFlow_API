using TaskFlow_API.DTOs;
using TaskFlow_API.Models;
using TaskFlow_API.Repositories;

namespace TaskFlow_API.Services;

public interface ISavedFilterService
{
    System.Threading.Tasks.Task<IEnumerable<SavedFilterDto>> GetByUserAsync(Guid userId);
    System.Threading.Tasks.Task<SavedFilterDto?> GetByIdAsync(Guid id, Guid userId);
    System.Threading.Tasks.Task<SavedFilterDto> CreateAsync(Guid userId, CreateSavedFilterDto dto);
    System.Threading.Tasks.Task<SavedFilterDto?> UpdateAsync(Guid id, Guid userId, UpdateSavedFilterDto dto);
    System.Threading.Tasks.Task<bool> DeleteAsync(Guid id, Guid userId);
}

public class SavedFilterService : ISavedFilterService
{
    private readonly IUnitOfWork _uow;

    public SavedFilterService(IUnitOfWork uow) => _uow = uow;

    public async System.Threading.Tasks.Task<IEnumerable<SavedFilterDto>> GetByUserAsync(Guid userId)
    {
        var items = await _uow.SavedFilters.GetByUserAsync(userId);
        return items.Select(MapToDto).ToList();
    }

    public async System.Threading.Tasks.Task<SavedFilterDto?> GetByIdAsync(Guid id, Guid userId)
    {
        var entity = await _uow.SavedFilters.GetByIdAsync(id);
        if (entity == null || entity.UserId != userId) return null;
        return MapToDto(entity);
    }

    public async System.Threading.Tasks.Task<SavedFilterDto> CreateAsync(Guid userId, CreateSavedFilterDto dto)
    {
        var entity = new SavedFilter
        {
            UserId = userId,
            Name = dto.Name,
            FilterCriteria = dto.FilterCriteria
        };
        await _uow.SavedFilters.AddAsync(entity);
        await _uow.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async System.Threading.Tasks.Task<SavedFilterDto?> UpdateAsync(Guid id, Guid userId, UpdateSavedFilterDto dto)
    {
        var entity = await _uow.SavedFilters.GetByIdAsync(id);
        if (entity == null || entity.UserId != userId) return null;

        if (!string.IsNullOrEmpty(dto.Name)) entity.Name = dto.Name;
        if (!string.IsNullOrEmpty(dto.FilterCriteria)) entity.FilterCriteria = dto.FilterCriteria;
        entity.UpdatedAt = DateTime.UtcNow;

        await _uow.SavedFilters.UpdateAsync(entity);
        await _uow.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async System.Threading.Tasks.Task<bool> DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _uow.SavedFilters.GetByIdAsync(id);
        if (entity == null || entity.UserId != userId) return false;
        await _uow.SavedFilters.DeleteAsync(entity);
        await _uow.SaveChangesAsync();
        return true;
    }

    private static SavedFilterDto MapToDto(SavedFilter sf) => new()
    {
        Id = sf.Id,
        UserId = sf.UserId,
        Name = sf.Name,
        FilterCriteria = sf.FilterCriteria,
        CreatedAt = sf.CreatedAt,
        UpdatedAt = sf.UpdatedAt
    };
}
