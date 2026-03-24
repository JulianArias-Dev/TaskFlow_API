using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow_API.DTOs;
using TaskFlow_API.Services;

namespace TaskFlow_API.Controllers;

/// <summary>
/// Controller para gestionar operaciones de columnas (Kanban columns)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class ColumnsController : ControllerBase
{
    private readonly IColumnService _columnService;
    private readonly IMapper _mapper;
    private readonly ILogger<ColumnsController> _logger;

    public ColumnsController(IColumnService columnService, IMapper mapper, ILogger<ColumnsController> logger)
    {
        _columnService = columnService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene una columna por ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ResponseDto<ColumnDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseDto<ColumnDto>>> GetColumnById(Guid id)
    {
        var column = await _columnService.GetColumnByIdAsync(id);
        if (column == null)
        {
            return NotFound(ResponseDto.NotFoundResponse($"Column with ID {id} not found"));
        }
        return Ok(ResponseDto<ColumnDto>.SuccessResponse(column, "Column retrieved successfully"));
    }

    /// <summary>
    /// Obtiene columnas de un board
    /// </summary>
    [HttpGet("board/{boardId}")]
    [ProducesResponseType(typeof(ResponseDto<List<ColumnDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDto<List<ColumnDto>>>> GetColumnsByBoard(Guid boardId)
    {
        var columns = await _columnService.GetColumnsByBoardAsync(boardId);
        return Ok(ResponseDto<List<ColumnDto>>.SuccessResponse(columns.ToList(), "Columns retrieved successfully"));
    }

    /// <summary>
    /// Obtiene columnas con paginación
    /// </summary>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResponseDto<ColumnDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponseDto<ColumnDto>>> GetColumnsPaged(
        [FromQuery] Guid boardId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;

        var (items, totalCount) = await _columnService.GetColumnsPagedByBoardAsync(boardId, pageNumber, pageSize);
        return Ok(PagedResponseDto<ColumnDto>.SuccessResponse(items, pageNumber, pageSize, totalCount));
    }

    /// <summary>
    /// Crea una nueva columna
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ResponseDto<ColumnDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto<ColumnDto>>> CreateColumn(CreateColumnDto createColumnDto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
            return BadRequest(ResponseDto.ValidationErrorResponse(errors));
        }

        try
        {
            var createdColumn = await _columnService.CreateColumnAsync(createColumnDto);
            _logger.LogInformation("Column '{ColumnName}' created", createdColumn.Name);
            return CreatedAtAction(nameof(GetColumnById), new { id = createdColumn.Id },
                ResponseDto<ColumnDto>.SuccessResponse(createdColumn, "Column created successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(ResponseDto.ErrorResponse(ex.Message, new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Actualiza una columna
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ResponseDto<ColumnDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseDto<ColumnDto>>> UpdateColumn(Guid id, UpdateColumnDto updateColumnDto)
    {
        try
        {
            var updatedColumn = await _columnService.UpdateColumnAsync(id, updateColumnDto);
            _logger.LogInformation("Column {ColumnId} updated", id);
            return Ok(ResponseDto<ColumnDto>.SuccessResponse(updatedColumn, "Column updated successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ResponseDto.NotFoundResponse(ex.Message));
        }
    }

    /// <summary>
    /// Elimina una columna
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto>> DeleteColumn(Guid id)
    {
        try
        {
            var result = await _columnService.DeleteColumnAsync(id);
            if (!result)
            {
                return NotFound(ResponseDto.NotFoundResponse($"Column with ID {id} not found"));
            }
            _logger.LogInformation("Column {ColumnId} deleted", id);
            return Ok(ResponseDto.SuccessResponse("Column deleted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cannot delete column {ColumnId}: {Message}", id, ex.Message);
            return BadRequest(ResponseDto.ErrorResponse(ex.Message, new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Verifica WIP Limit de una columna
    /// </summary>
    [HttpGet("{columnId}/wip-check")]
    [ProducesResponseType(typeof(ResponseDto<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDto<bool>>> CheckWipLimit(Guid columnId)
    {
        var canAddTask = await _columnService.CheckWipLimitAsync(columnId);
        return Ok(ResponseDto<bool>.SuccessResponse(canAddTask, 
            canAddTask ? "WIP Limit OK" : "WIP Limit exceeded"));
    }

    /// <summary>
    /// Obtiene el conteo de tareas en una columna
    /// </summary>
    [HttpGet("{columnId}/task-count")]
    [ProducesResponseType(typeof(ResponseDto<int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDto<int>>> GetTaskCount(Guid columnId)
    {
        var count = await _columnService.GetTaskCountByColumnAsync(columnId);
        return Ok(ResponseDto<int>.SuccessResponse(count, "Task count retrieved successfully"));
    }
}
