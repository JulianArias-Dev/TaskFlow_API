using AutoMapper;
using TaskFlow_API.DTOs;
using TaskFlow_API.Models;
using TaskFlow_API.Repositories;

namespace TaskFlow_API.Services;

/// <summary>
/// Interfaz del servicio de Tag
/// </summary>
public interface ITagService
{
    System.Threading.Tasks.Task<TagDto?> GetTagByIdAsync(Guid id);
    System.Threading.Tasks.Task<IEnumerable<TagDto>> GetTagsByProjectAsync(Guid projectId);
    System.Threading.Tasks.Task<(List<TagDto> items, int totalCount)> GetTagsPagedByProjectAsync(Guid projectId, int pageNumber, int pageSize);
    System.Threading.Tasks.Task<IEnumerable<TagDto>> SearchTagsAsync(string searchTerm, Guid projectId);
    System.Threading.Tasks.Task<TagDto> CreateTagAsync(CreateTagDto createTagDto);
    System.Threading.Tasks.Task<TagDto> UpdateTagAsync(Guid id, UpdateTagDto updateTagDto);
    System.Threading.Tasks.Task<bool> DeleteTagAsync(Guid id);
    System.Threading.Tasks.Task<int> GetTaskCountByTagAsync(Guid tagId);
}

/// <summary>
/// Servicio de Tag
/// </summary>
public class TagService : ITagService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<TagService> _logger;

    public TagService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<TagService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async System.Threading.Tasks.Task<TagDto?> GetTagByIdAsync(Guid id)
    {
        var tag = await _unitOfWork.Tags.GetByIdAsync(id);
        return tag != null ? _mapper.Map<TagDto>(tag) : null;
    }

    public async System.Threading.Tasks.Task<IEnumerable<TagDto>> GetTagsByProjectAsync(Guid projectId)
    {
        var tags = await _unitOfWork.Tags.GetTagsByProjectAsync(projectId);
        return _mapper.Map<IEnumerable<TagDto>>(tags);
    }

    public async System.Threading.Tasks.Task<(List<TagDto> items, int totalCount)> GetTagsPagedByProjectAsync(Guid projectId, int pageNumber, int pageSize)
    {
        var (items, totalCount) = await _unitOfWork.Tags.GetTagsPagedByProjectAsync(projectId, pageNumber, pageSize);
        var mappedItems = _mapper.Map<List<TagDto>>(items);
        return (mappedItems, totalCount);
    }

    public async System.Threading.Tasks.Task<IEnumerable<TagDto>> SearchTagsAsync(string searchTerm, Guid projectId)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetTagsByProjectAsync(projectId);
        }

        var tags = await _unitOfWork.Tags.SearchTagsAsync(searchTerm, projectId);
        return _mapper.Map<IEnumerable<TagDto>>(tags);
    }

    public async System.Threading.Tasks.Task<TagDto> CreateTagAsync(CreateTagDto createTagDto)
    {
        // Validar que el proyecto existe
        if (!await _unitOfWork.Projects.ExistsAsync(createTagDto.ProjectId))
        {
            throw new KeyNotFoundException($"Project with ID {createTagDto.ProjectId} not found");
        }

        // Validar que la etiqueta no existe
        var existingTag = await _unitOfWork.Tags.GetTagByNameAsync(createTagDto.Name, createTagDto.ProjectId);
        if (existingTag != null)
        {
            throw new InvalidOperationException($"Tag '{createTagDto.Name}' already exists in this project");
        }

        var tag = _mapper.Map<Tag>(createTagDto);
        
        await _unitOfWork.Tags.AddAsync(tag);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Tag '{TagName}' created in project {ProjectId}", tag.Name, tag.ProjectId);
        return _mapper.Map<TagDto>(tag);
    }

    public async System.Threading.Tasks.Task<TagDto> UpdateTagAsync(Guid id, UpdateTagDto updateTagDto)
    {
        var tag = await _unitOfWork.Tags.GetByIdAsync(id);
        if (tag == null)
        {
            throw new KeyNotFoundException($"Tag with ID {id} not found");
        }

        if (!string.IsNullOrEmpty(updateTagDto.Name))
        {
            // Validar que el nuevo nombre no existe
            var existingTag = await _unitOfWork.Tags.GetTagByNameAsync(updateTagDto.Name, tag.ProjectId);
            if (existingTag != null && existingTag.Id != id)
            {
                throw new InvalidOperationException($"Tag '{updateTagDto.Name}' already exists in this project");
            }
            tag.Name = updateTagDto.Name;
        }

        if (!string.IsNullOrEmpty(updateTagDto.Color))
            tag.Color = updateTagDto.Color;

        await _unitOfWork.Tags.UpdateAsync(tag);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Tag {TagId} updated", id);
        return _mapper.Map<TagDto>(tag);
    }

    public async System.Threading.Tasks.Task<bool> DeleteTagAsync(Guid id)
    {
        var tag = await _unitOfWork.Tags.GetByIdAsync(id);
        if (tag == null)
        {
            return false;
        }

        // Verificar si hay tareas con esta etiqueta
        var taskCount = await _unitOfWork.Tags.GetTaskCountByTagAsync(id);
        if (taskCount > 0)
        {
            _logger.LogWarning("Cannot delete tag {TagId} - associated with {TaskCount} tasks", id, taskCount);
            throw new InvalidOperationException($"Cannot delete tag - {taskCount} tasks are using this tag. Please remove the tag from tasks first.");
        }

        await _unitOfWork.Tags.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Tag {TagId} deleted", id);
        return true;
    }

    public async System.Threading.Tasks.Task<int> GetTaskCountByTagAsync(Guid tagId)
    {
        return await _unitOfWork.Tags.GetTaskCountByTagAsync(tagId);
    }
}
