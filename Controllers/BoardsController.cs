using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow_API.DTOs;
using TaskFlow_API.Services;

namespace TaskFlow_API.Controllers;

/// <summary>
/// Controller para gestionar operaciones de boards (Kanban boards)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class BoardsController : ControllerBase
{
    private readonly IBoardService _boardService;
    private readonly IMapper _mapper;
    private readonly ILogger<BoardsController> _logger;

    public BoardsController(IBoardService boardService, IMapper mapper, ILogger<BoardsController> logger)
    {
        _boardService = boardService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene un board por ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ResponseDto<BoardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseDto<BoardDto>>> GetBoardById(Guid id)
    {
        var board = await _boardService.GetBoardByIdAsync(id);
        if (board == null)
        {
            return NotFound(ResponseDto.NotFoundResponse($"Board with ID {id} not found"));
        }
        return Ok(ResponseDto<BoardDto>.SuccessResponse(board, "Board retrieved successfully"));
    }

    /// <summary>
    /// Obtiene boards de un proyecto
    /// </summary>
    [HttpGet("project/{projectId}")]
    [ProducesResponseType(typeof(ResponseDto<List<BoardDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDto<List<BoardDto>>>> GetBoardsByProject(Guid projectId)
    {
        var boards = await _boardService.GetBoardsByProjectAsync(projectId);
        return Ok(ResponseDto<List<BoardDto>>.SuccessResponse(boards.ToList(), "Boards retrieved successfully"));
    }

    /// <summary>
    /// Obtiene boards con paginación
    /// </summary>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResponseDto<BoardDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponseDto<BoardDto>>> GetBoardsPaged(
        [FromQuery] Guid projectId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;

        var (items, totalCount) = await _boardService.GetBoardsPagedByProjectAsync(projectId, pageNumber, pageSize);
        return Ok(PagedResponseDto<BoardDto>.SuccessResponse(items, pageNumber, pageSize, totalCount));
    }

    /// <summary>
    /// Crea un nuevo board
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ResponseDto<BoardDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto<BoardDto>>> CreateBoard(CreateBoardDto createBoardDto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
            return BadRequest(ResponseDto.ValidationErrorResponse(errors));
        }

        try
        {
            var createdBoard = await _boardService.CreateBoardAsync(createBoardDto);
            _logger.LogInformation("Board '{BoardName}' created", createdBoard.Name);
            return CreatedAtAction(nameof(GetBoardById), new { id = createdBoard.Id },
                ResponseDto<BoardDto>.SuccessResponse(createdBoard, "Board created successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(ResponseDto.ErrorResponse(ex.Message, new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Actualiza un board
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ResponseDto<BoardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseDto<BoardDto>>> UpdateBoard(Guid id, UpdateBoardDto updateBoardDto)
    {
        try
        {
            var updatedBoard = await _boardService.UpdateBoardAsync(id, updateBoardDto);
            _logger.LogInformation("Board {BoardId} updated", id);
            return Ok(ResponseDto<BoardDto>.SuccessResponse(updatedBoard, "Board updated successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ResponseDto.NotFoundResponse(ex.Message));
        }
    }

    /// <summary>
    /// Elimina un board
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseDto>> DeleteBoard(Guid id)
    {
        var result = await _boardService.DeleteBoardAsync(id);
        if (!result)
        {
            return NotFound(ResponseDto.NotFoundResponse($"Board with ID {id} not found"));
        }
        _logger.LogInformation("Board {BoardId} deleted", id);
        return Ok(ResponseDto.SuccessResponse("Board deleted successfully"));
    }
}
