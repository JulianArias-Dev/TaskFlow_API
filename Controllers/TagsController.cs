using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow_API.DTOs;
using TaskFlow_API.Services;

namespace TaskFlow_API.Controllers;

/// <summary>
/// Controller para gestionar etiquetas
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class TagsController : ControllerBase
{
    private readonly ITagService _tagService;
    private readonly IMapper _mapper;
    private readonly ILogger<TagsController> _logger;

    public TagsController(ITagService tagService, IMapper mapper, ILogger<TagsController> logger)
    {
        _tagService = tagService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene una etiqueta por ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ResponseDto<TagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseDto<TagDto>>> GetTagById(Guid id)
    {
        var tag = await _tagService.GetTagByIdAsync(id);
        if (tag == null)
        {
            return NotFound(ResponseDto.NotFoundResponse($"Tag with ID {id} not found"));
        }
        return Ok(ResponseDto<TagDto>.SuccessResponse(tag, "Tag retrieved successfully"));
    }

    /// <summary>
    /// Obtiene etiquetas de un proyecto
    /// </summary>
    [HttpGet("project/{projectId}")]
    [ProducesResponseType(typeof(ResponseDto<List<TagDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDto<List<TagDto>>>> GetTagsByProject(Guid projectId)
    {
        var tags = await _tagService.GetTagsByProjectAsync(projectId);
        return Ok(ResponseDto<List<TagDto>>.SuccessResponse(tags.ToList(), "Tags retrieved successfully"));
    }

    /// <summary>
    /// Obtiene etiquetas con paginación
    /// </summary>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResponseDto<TagDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponseDto<TagDto>>> GetTagsPaged(
        [FromQuery] Guid projectId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;

        var (items, totalCount) = await _tagService.GetTagsPagedByProjectAsync(projectId, pageNumber, pageSize);
        return Ok(PagedResponseDto<TagDto>.SuccessResponse(items, pageNumber, pageSize, totalCount));
    }

    /// <summary>
    /// Busca etiquetas por nombre
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ResponseDto<List<TagDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDto<List<TagDto>>>> SearchTags(
        [FromQuery] string searchTerm,
        [FromQuery] Guid projectId)
    {
        var tags = await _tagService.SearchTagsAsync(searchTerm, projectId);
        return Ok(ResponseDto<List<TagDto>>.SuccessResponse(tags.ToList(), "Search results retrieved successfully"));
    }

    /// <summary>
    /// Crea una nueva etiqueta
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ResponseDto<TagDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto<TagDto>>> CreateTag(CreateTagDto createTagDto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
            return BadRequest(ResponseDto.ValidationErrorResponse(errors));
        }

        try
        {
            var createdTag = await _tagService.CreateTagAsync(createTagDto);
            _logger.LogInformation("Tag '{TagName}' created", createdTag.Name);
            return CreatedAtAction(nameof(GetTagById), new { id = createdTag.Id },
                ResponseDto<TagDto>.SuccessResponse(createdTag, "Tag created successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(ResponseDto.ErrorResponse(ex.Message, new List<string> { ex.Message }));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto.ErrorResponse(ex.Message, new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Actualiza una etiqueta
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ResponseDto<TagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseDto<TagDto>>> UpdateTag(Guid id, UpdateTagDto updateTagDto)
    {
        try
        {
            var updatedTag = await _tagService.UpdateTagAsync(id, updateTagDto);
            _logger.LogInformation("Tag {TagId} updated", id);
            return Ok(ResponseDto<TagDto>.SuccessResponse(updatedTag, "Tag updated successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ResponseDto.NotFoundResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseDto.ErrorResponse(ex.Message, new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Elimina una etiqueta
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto>> DeleteTag(Guid id)
    {
        try
        {
            var result = await _tagService.DeleteTagAsync(id);
            if (!result)
            {
                return NotFound(ResponseDto.NotFoundResponse($"Tag with ID {id} not found"));
            }
            _logger.LogInformation("Tag {TagId} deleted", id);
            return Ok(ResponseDto.SuccessResponse("Tag deleted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cannot delete tag {TagId}: {Message}", id, ex.Message);
            return BadRequest(ResponseDto.ErrorResponse(ex.Message, new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Obtiene el conteo de tareas con una etiqueta
    /// </summary>
    [HttpGet("{tagId}/task-count")]
    [ProducesResponseType(typeof(ResponseDto<int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDto<int>>> GetTaskCount(Guid tagId)
    {
        var count = await _tagService.GetTaskCountByTagAsync(tagId);
        return Ok(ResponseDto<int>.SuccessResponse(count, "Task count retrieved successfully"));
    }
}
