using AutoMapper;
using TaskFlow_API.DTOs;
using TaskFlow_API.Models;
using TaskFlow_API.Repositories;

namespace TaskFlow_API.Services;

/// <summary>
/// Interfaz del servicio de Column
/// </summary>
public interface IColumnService
{
    System.Threading.Tasks.Task<ColumnDto?> GetColumnByIdAsync(Guid id);
    System.Threading.Tasks.Task<IEnumerable<ColumnDto>> GetColumnsByBoardAsync(Guid boardId);
    System.Threading.Tasks.Task<(List<ColumnDto> items, int totalCount)> GetColumnsPagedByBoardAsync(Guid boardId, int pageNumber, int pageSize);
    System.Threading.Tasks.Task<ColumnDto> CreateColumnAsync(CreateColumnDto createColumnDto);
    System.Threading.Tasks.Task<ColumnDto> UpdateColumnAsync(Guid id, UpdateColumnDto updateColumnDto);
    System.Threading.Tasks.Task<bool> DeleteColumnAsync(Guid id);
    System.Threading.Tasks.Task<int> GetTaskCountByColumnAsync(Guid columnId);
    System.Threading.Tasks.Task<bool> CheckWipLimitAsync(Guid columnId);
}

/// <summary>
/// Servicio de Column
/// </summary>
public class ColumnService : IColumnService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ColumnService> _logger;

    public ColumnService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ColumnService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async System.Threading.Tasks.Task<ColumnDto?> GetColumnByIdAsync(Guid id)
    {
        var column = await _unitOfWork.Columns.GetColumnWithTasksAsync(id);
        return column != null ? _mapper.Map<ColumnDto>(column) : null;
    }

    public async System.Threading.Tasks.Task<IEnumerable<ColumnDto>> GetColumnsByBoardAsync(Guid boardId)
    {
        var columns = await _unitOfWork.Columns.GetColumnsByBoardAsync(boardId);
        return _mapper.Map<IEnumerable<ColumnDto>>(columns);
    }

    public async System.Threading.Tasks.Task<(List<ColumnDto> items, int totalCount)> GetColumnsPagedByBoardAsync(Guid boardId, int pageNumber, int pageSize)
    {
        var (items, totalCount) = await _unitOfWork.Columns.GetColumnsPagedByBoardAsync(boardId, pageNumber, pageSize);
        var mappedItems = _mapper.Map<List<ColumnDto>>(items);
        return (mappedItems, totalCount);
    }

	public async System.Threading.Tasks.Task<ColumnDto> CreateColumnAsync(CreateColumnDto createColumnDto)
	{
		var board = await _unitOfWork.Boards.GetByIdAsync(createColumnDto.BoardId);
		if (board == null)
		{
			throw new KeyNotFoundException($"Board with ID {createColumnDto.BoardId} not found");
		}

		// Calculamos el siguiente DisplayOrder
		var existingColumns = await _unitOfWork.Columns.FindAsync(c => c.BoardId == createColumnDto.BoardId);
		int nextOrder = existingColumns.Any() ? existingColumns.Max(c => c.DisplayOrder) + 1 : 0;

		var column = _mapper.Map<Column>(createColumnDto);
		column.DisplayOrder = nextOrder; // Forzamos que vaya al final

		await _unitOfWork.Columns.AddAsync(column);
		await _unitOfWork.SaveChangesAsync();

		return _mapper.Map<ColumnDto>(column);
	}

	public async System.Threading.Tasks.Task<ColumnDto> UpdateColumnAsync(Guid id, UpdateColumnDto updateColumnDto)
    {
        var column = await _unitOfWork.Columns.GetByIdAsync(id);
        if (column == null)
        {
            throw new KeyNotFoundException($"Column with ID {id} not found");
        }

		if (updateColumnDto.DisplayOrder.HasValue && updateColumnDto.DisplayOrder.Value != column.DisplayOrder)
		{
			var allColumns = (await _unitOfWork.Columns.FindAsync(c => c.BoardId == column.BoardId))
							 .OrderBy(c => c.DisplayOrder).ToList();

			int oldOrder = column.DisplayOrder;
			int newOrder = updateColumnDto.DisplayOrder.Value;

			// Ajustamos los órdenes de las DEMÁS columnas
			foreach (var col in allColumns.Where(c => c.Id != id))
			{
				if (newOrder < oldOrder) // Se movió hacia arriba
				{
					if (col.DisplayOrder >= newOrder && col.DisplayOrder < oldOrder)
						col.DisplayOrder++;
				}
				else // Se movió hacia abajo
				{
					if (col.DisplayOrder > oldOrder && col.DisplayOrder <= newOrder)
						col.DisplayOrder--;
				}
			}
			column.DisplayOrder = newOrder;
		}

		if (!string.IsNullOrEmpty(updateColumnDto.Name))
            column.Name = updateColumnDto.Name;

        if (updateColumnDto.WipLimit.HasValue)
            column.WipLimit = updateColumnDto.WipLimit;

        if (!string.IsNullOrEmpty(updateColumnDto.Color))
            column.Color = updateColumnDto.Color;

        if (updateColumnDto.DisplayOrder.HasValue)
            column.DisplayOrder = updateColumnDto.DisplayOrder.Value;

        column.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Columns.UpdateAsync(column);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Column {ColumnId} updated", id);
        return _mapper.Map<ColumnDto>(column);
    }

	public async System.Threading.Tasks.Task<bool> DeleteColumnAsync(Guid id)
	{
		var column = await _unitOfWork.Columns.GetByIdAsync(id);
		if (column == null) return false;

		// Validación de tareas (la que ya tenías)
		var taskCount = await _unitOfWork.Columns.GetTaskCountByColumnAsync(id);
		if (taskCount > 0) throw new InvalidOperationException("Mueva las tareas antes de eliminar.");

		int removedOrder = column.DisplayOrder;
		Guid boardId = column.BoardId;

		await _unitOfWork.Columns.DeleteAsync(id);

		// REORGANIZAR: Traemos las que quedaron con un orden mayor al eliminado
		var affectedColumns = await _unitOfWork.Columns.FindAsync(c => c.BoardId == boardId && c.DisplayOrder > removedOrder);

		foreach (var col in affectedColumns)
		{
			col.DisplayOrder--; // Cerramos el hueco
		}

		await _unitOfWork.SaveChangesAsync();
		return true;
	}

	public async System.Threading.Tasks.Task<int> GetTaskCountByColumnAsync(Guid columnId)
    {
        return await _unitOfWork.Columns.GetTaskCountByColumnAsync(columnId);
    }

    public async System.Threading.Tasks.Task<bool> CheckWipLimitAsync(Guid columnId)
    {
        var column = await _unitOfWork.Columns.GetByIdAsync(columnId);
        if (column == null || !column.WipLimit.HasValue)
        {
            return true; // Sin límite WIP
        }

        var taskCount = await _unitOfWork.Columns.GetTaskCountByColumnAsync(columnId);
        return taskCount < column.WipLimit;
    }
}
