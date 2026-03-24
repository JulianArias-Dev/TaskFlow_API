using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow_API.DTOs;
using TaskFlow_API.Services;

namespace TaskFlow_API.Controllers;

/// <summary>
/// Controller para gestionar comentarios
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;
    private readonly IMapper _mapper;
    private readonly ILogger<CommentsController> _logger;

    public CommentsController(ICommentService commentService, IMapper mapper, ILogger<CommentsController> logger)
    {
        _commentService = commentService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene un comentario por ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ResponseDto<CommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseDto<CommentDto>>> GetCommentById(Guid id)
    {
        var comment = await _commentService.GetCommentByIdAsync(id);
        if (comment == null)
        {
            return NotFound(ResponseDto.NotFoundResponse($"Comment with ID {id} not found"));
        }
        return Ok(ResponseDto<CommentDto>.SuccessResponse(comment, "Comment retrieved successfully"));
    }

    /// <summary>
    /// Obtiene comentarios de una tarea
    /// </summary>
    [HttpGet("task/{taskId}")]
    [ProducesResponseType(typeof(ResponseDto<List<CommentDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDto<List<CommentDto>>>> GetCommentsByTask(Guid taskId)
    {
        var comments = await _commentService.GetCommentsByTaskAsync(taskId);
        return Ok(ResponseDto<List<CommentDto>>.SuccessResponse(comments.ToList(), "Comments retrieved successfully"));
    }

    /// <summary>
    /// Obtiene comentarios de una tarea con paginación
    /// </summary>
    [HttpGet("task/{taskId}/paged")]
    [ProducesResponseType(typeof(PagedResponseDto<CommentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponseDto<CommentDto>>> GetCommentsByTaskPaged(
        Guid taskId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;

        var (items, totalCount) = await _commentService.GetCommentsPagedByTaskAsync(taskId, pageNumber, pageSize);
        return Ok(PagedResponseDto<CommentDto>.SuccessResponse(items, pageNumber, pageSize, totalCount));
    }

    /// <summary>
    /// Obtiene comentarios de un usuario
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(ResponseDto<List<CommentDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDto<List<CommentDto>>>> GetCommentsByUser(Guid userId)
    {
        var comments = await _commentService.GetCommentsByUserAsync(userId);
        return Ok(ResponseDto<List<CommentDto>>.SuccessResponse(comments.ToList(), "User comments retrieved successfully"));
    }

    /// <summary>
    /// Crea un nuevo comentario
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ResponseDto<CommentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto<CommentDto>>> CreateComment(CreateCommentDto createCommentDto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
            return BadRequest(ResponseDto.ValidationErrorResponse(errors));
        }

        try
        {
            var createdComment = await _commentService.CreateCommentAsync(createCommentDto);
            _logger.LogInformation("Comment created on task {TaskId}", createCommentDto.TaskId);
            return CreatedAtAction(nameof(GetCommentById), new { id = createdComment.Id },
                ResponseDto<CommentDto>.SuccessResponse(createdComment, "Comment created successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(ResponseDto.ErrorResponse(ex.Message, new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Actualiza un comentario
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ResponseDto<CommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseDto<CommentDto>>> UpdateComment(Guid id, UpdateCommentDto updateCommentDto)
    {
        try
        {
            var updatedComment = await _commentService.UpdateCommentAsync(id, updateCommentDto);
            _logger.LogInformation("Comment {CommentId} updated", id);
            return Ok(ResponseDto<CommentDto>.SuccessResponse(updatedComment, "Comment updated successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ResponseDto.NotFoundResponse(ex.Message));
        }
    }

    /// <summary>
    /// Elimina un comentario
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseDto>> DeleteComment(Guid id)
    {
        var result = await _commentService.DeleteCommentAsync(id);
        if (!result)
        {
            return NotFound(ResponseDto.NotFoundResponse($"Comment with ID {id} not found"));
        }
        _logger.LogInformation("Comment {CommentId} deleted", id);
        return Ok(ResponseDto.SuccessResponse("Comment deleted successfully"));
    }

    /// <summary>
    /// Obtiene el conteo de comentarios en una tarea
    /// </summary>
    [HttpGet("task/{taskId}/count")]
    [ProducesResponseType(typeof(ResponseDto<int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDto<int>>> GetCommentCount(Guid taskId)
    {
        var count = await _commentService.GetCommentCountByTaskAsync(taskId);
        return Ok(ResponseDto<int>.SuccessResponse(count, "Comment count retrieved successfully"));
    }
}
