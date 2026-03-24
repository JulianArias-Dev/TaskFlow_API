using AutoMapper;
using TaskFlow_API.DTOs;
using TaskFlow_API.Models;
using TaskFlow_API.Repositories;

namespace TaskFlow_API.Services;

/// <summary>
/// Interfaz del servicio de Board
/// </summary>
public interface IBoardService
{
    System.Threading.Tasks.Task<BoardDto?> GetBoardByIdAsync(Guid id);
    System.Threading.Tasks.Task<IEnumerable<BoardDto>> GetBoardsByProjectAsync(Guid projectId);
    System.Threading.Tasks.Task<(List<BoardDto> items, int totalCount)> GetBoardsPagedByProjectAsync(Guid projectId, int pageNumber, int pageSize);
    System.Threading.Tasks.Task<BoardDto> CreateBoardAsync(CreateBoardDto createBoardDto);
    System.Threading.Tasks.Task<BoardDto> UpdateBoardAsync(Guid id, UpdateBoardDto updateBoardDto);
    System.Threading.Tasks.Task<bool> DeleteBoardAsync(Guid id);
    System.Threading.Tasks.Task<int> GetBoardCountByProjectAsync(Guid projectId);
}

/// <summary>
/// Servicio de Board
/// </summary>
public class BoardService : IBoardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<BoardService> _logger;

    public BoardService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<BoardService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async System.Threading.Tasks.Task<BoardDto?> GetBoardByIdAsync(Guid id)
    {
        var board = await _unitOfWork.Boards.GetBoardWithColumnsAsync(id);
        return board != null ? _mapper.Map<BoardDto>(board) : null;
    }

    public async System.Threading.Tasks.Task<IEnumerable<BoardDto>> GetBoardsByProjectAsync(Guid projectId)
    {
        var boards = await _unitOfWork.Boards.GetBoardsByProjectAsync(projectId);
        return _mapper.Map<IEnumerable<BoardDto>>(boards);
    }

    public async System.Threading.Tasks.Task<(List<BoardDto> items, int totalCount)> GetBoardsPagedByProjectAsync(Guid projectId, int pageNumber, int pageSize)
    {
        var (items, totalCount) = await _unitOfWork.Boards.GetBoardsPagedByProjectAsync(projectId, pageNumber, pageSize);
        var mappedItems = _mapper.Map<List<BoardDto>>(items);
        return (mappedItems, totalCount);
    }

    public async System.Threading.Tasks.Task<BoardDto> CreateBoardAsync(CreateBoardDto createBoardDto)
    {
        if (!await _unitOfWork.Projects.ExistsAsync(createBoardDto.ProjectId))
        {
            throw new KeyNotFoundException($"Project with ID {createBoardDto.ProjectId} not found");
        }

        var board = _mapper.Map<Board>(createBoardDto);
        
        await _unitOfWork.Boards.AddAsync(board);
        await _unitOfWork.SaveChangesAsync();
        
        _logger.LogInformation("Board '{BoardName}' created for project {ProjectId}", board.Name, board.ProjectId);
        return _mapper.Map<BoardDto>(board);
    }

    public async System.Threading.Tasks.Task<BoardDto> UpdateBoardAsync(Guid id, UpdateBoardDto updateBoardDto)
    {
        var board = await _unitOfWork.Boards.GetByIdAsync(id);
        if (board == null)
        {
            throw new KeyNotFoundException($"Board with ID {id} not found");
        }

        if (!string.IsNullOrEmpty(updateBoardDto.Name))
            board.Name = updateBoardDto.Name;

        if (!string.IsNullOrEmpty(updateBoardDto.Description))
            board.Description = updateBoardDto.Description;

        if (updateBoardDto.DisplayOrder.HasValue)
            board.DisplayOrder = updateBoardDto.DisplayOrder.Value;

        board.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Boards.UpdateAsync(board);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Board {BoardId} updated", id);
        return _mapper.Map<BoardDto>(board);
    }

    public async System.Threading.Tasks.Task<bool> DeleteBoardAsync(Guid id)
    {
        var board = await _unitOfWork.Boards.GetByIdAsync(id);
        if (board == null)
        {
            return false;
        }

        await _unitOfWork.Boards.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Board {BoardId} deleted", id);
        return true;
    }

    public async System.Threading.Tasks.Task<int> GetBoardCountByProjectAsync(Guid projectId)
    {
        var boards = await _unitOfWork.Boards.GetBoardsByProjectAsync(projectId);
        return boards.Count();
    }
}
