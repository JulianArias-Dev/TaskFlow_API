using AutoMapper;
using TaskFlow_API.DTOs;
using TaskFlow_API.Models;
using TaskFlow_API.Repositories;

namespace TaskFlow_API.Services;

/// <summary>
/// Interfaz del servicio de Comment
/// </summary>
public interface ICommentService
{
    System.Threading.Tasks.Task<CommentDto?> GetCommentByIdAsync(Guid id);
    System.Threading.Tasks.Task<IEnumerable<CommentDto>> GetCommentsByTaskAsync(Guid taskId);
    System.Threading.Tasks.Task<(List<CommentDto> items, int totalCount)> GetCommentsPagedByTaskAsync(Guid taskId, int pageNumber, int pageSize);
    System.Threading.Tasks.Task<IEnumerable<CommentDto>> GetCommentsByUserAsync(Guid userId);
    System.Threading.Tasks.Task<CommentDto> CreateCommentAsync(CreateCommentDto createCommentDto);
    System.Threading.Tasks.Task<CommentDto> UpdateCommentAsync(Guid id, UpdateCommentDto updateCommentDto);
    System.Threading.Tasks.Task<bool> DeleteCommentAsync(Guid id);
    System.Threading.Tasks.Task<int> GetCommentCountByTaskAsync(Guid taskId);
}

/// <summary>
/// Servicio de Comment
/// </summary>
public class CommentService : ICommentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CommentService> _logger;

    public CommentService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<CommentService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async System.Threading.Tasks.Task<CommentDto?> GetCommentByIdAsync(Guid id)
    {
        var comment = await _unitOfWork.Comments.GetByIdAsync(id);
        return comment != null ? _mapper.Map<CommentDto>(comment) : null;
    }

    public async System.Threading.Tasks.Task<IEnumerable<CommentDto>> GetCommentsByTaskAsync(Guid taskId)
    {
        var comments = await _unitOfWork.Comments.GetCommentsByTaskAsync(taskId);
        return _mapper.Map<IEnumerable<CommentDto>>(comments);
    }

    public async System.Threading.Tasks.Task<(List<CommentDto> items, int totalCount)> GetCommentsPagedByTaskAsync(Guid taskId, int pageNumber, int pageSize)
    {
        var (items, totalCount) = await _unitOfWork.Comments.GetCommentsPagedByTaskAsync(taskId, pageNumber, pageSize);
        var mappedItems = _mapper.Map<List<CommentDto>>(items);
        return (mappedItems, totalCount);
    }

    public async System.Threading.Tasks.Task<IEnumerable<CommentDto>> GetCommentsByUserAsync(Guid userId)
    {
        var comments = await _unitOfWork.Comments.GetCommentsByUserAsync(userId);
        return _mapper.Map<IEnumerable<CommentDto>>(comments);
    }

    public async System.Threading.Tasks.Task<CommentDto> CreateCommentAsync(CreateCommentDto createCommentDto)
    {
        // Validar que la tarea existe
        if (!await _unitOfWork.Tasks.ExistsAsync(createCommentDto.TaskId))
        {
            throw new KeyNotFoundException($"Task with ID {createCommentDto.TaskId} not found");
        }

        // Validar que el usuario existe
        if (!await _unitOfWork.Users.ExistsAsync(createCommentDto.UserId))
        {
            throw new KeyNotFoundException($"User with ID {createCommentDto.UserId} not found");
        }

        var comment = _mapper.Map<Comment>(createCommentDto);
        
        await _unitOfWork.Comments.AddAsync(comment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Comment created for task {TaskId} by user {UserId}", createCommentDto.TaskId, createCommentDto.UserId);
        
        // Recargar para obtener relaciones
        var createdComment = await _unitOfWork.Comments.GetByIdAsync(comment.Id);
        return _mapper.Map<CommentDto>(createdComment)!;
    }

    public async System.Threading.Tasks.Task<CommentDto> UpdateCommentAsync(Guid id, UpdateCommentDto updateCommentDto)
    {
        var comment = await _unitOfWork.Comments.GetByIdAsync(id);
        if (comment == null)
        {
            throw new KeyNotFoundException($"Comment with ID {id} not found");
        }

        comment.Content = updateCommentDto.Content;
        comment.UpdatedAt = DateTime.UtcNow;
        comment.IsEdited = true;

        await _unitOfWork.Comments.UpdateAsync(comment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Comment {CommentId} updated", id);

        // Recargar para obtener relaciones
        var updatedComment = await _unitOfWork.Comments.GetByIdAsync(id);
        return _mapper.Map<CommentDto>(updatedComment)!;
    }

    public async System.Threading.Tasks.Task<bool> DeleteCommentAsync(Guid id)
    {
        var comment = await _unitOfWork.Comments.GetByIdAsync(id);
        if (comment == null)
        {
            return false;
        }

        await _unitOfWork.Comments.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Comment {CommentId} deleted", id);
        return true;
    }

    public async System.Threading.Tasks.Task<int> GetCommentCountByTaskAsync(Guid taskId)
    {
        var comments = await _unitOfWork.Comments.GetCommentsByTaskAsync(taskId);
        return comments.Count();
    }
}
