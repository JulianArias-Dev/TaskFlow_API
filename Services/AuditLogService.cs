using AutoMapper;
using TaskFlow_API.DTOs;
using TaskFlow_API.Models;
using TaskFlow_API.Repositories;

namespace TaskFlow_API.Services;

/// <summary>
/// Interfaz del servicio de AuditLog
/// </summary>
public interface IAuditLogService
{
    System.Threading.Tasks.Task<AuditLogDto?> GetAuditLogByIdAsync(Guid id);
    System.Threading.Tasks.Task<IEnumerable<AuditLogDto>> GetLogsByEntityAsync(string entityType, Guid entityId);
    System.Threading.Tasks.Task<IEnumerable<AuditLogDto>> GetLogsByUserAsync(Guid userId);
    System.Threading.Tasks.Task<IEnumerable<AuditLogDto>> GetLogsByActionAsync(string action);
    System.Threading.Tasks.Task<PagedAuditLogDto> GetLogsPagedAsync(
        string? entityType = null, 
        Guid? entityId = null, 
        Guid? userId = null, 
        string? action = null,
        int pageNumber = 1, 
        int pageSize = 50);
    System.Threading.Tasks.Task LogActionAsync(string entityType, Guid entityId, string action, string? oldData = null, string? newData = null, Guid? userId = null, string? ipAddress = null);
}

/// <summary>
/// Servicio de AuditLog
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<AuditLogService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async System.Threading.Tasks.Task<AuditLogDto?> GetAuditLogByIdAsync(Guid id)
    {
        var auditLog = await _unitOfWork.AuditLogs.GetByIdAsync(id);
        return auditLog != null ? _mapper.Map<AuditLogDto>(auditLog) : null;
    }

    public async System.Threading.Tasks.Task<IEnumerable<AuditLogDto>> GetLogsByEntityAsync(string entityType, Guid entityId)
    {
        var logs = await _unitOfWork.AuditLogs.GetLogsByEntityAsync(entityType, entityId);
        return _mapper.Map<IEnumerable<AuditLogDto>>(logs);
    }

    public async System.Threading.Tasks.Task<IEnumerable<AuditLogDto>> GetLogsByUserAsync(Guid userId)
    {
        var logs = await _unitOfWork.AuditLogs.GetLogsByUserAsync(userId);
        return _mapper.Map<IEnumerable<AuditLogDto>>(logs);
    }

    public async System.Threading.Tasks.Task<IEnumerable<AuditLogDto>> GetLogsByActionAsync(string action)
    {
        var logs = await _unitOfWork.AuditLogs.GetLogsByActionAsync(action);
        return _mapper.Map<IEnumerable<AuditLogDto>>(logs);
    }

    public async System.Threading.Tasks.Task<PagedAuditLogDto> GetLogsPagedAsync(
        string? entityType = null, 
        Guid? entityId = null, 
        Guid? userId = null, 
        string? action = null,
        int pageNumber = 1, 
        int pageSize = 50)
    {
        var (items, totalCount) = await _unitOfWork.AuditLogs.GetLogsPagedAsync(entityType, entityId, userId, action, pageNumber, pageSize);
        var mappedItems = _mapper.Map<List<AuditLogDto>>(items);
        
        return new PagedAuditLogDto
        {
            Items = mappedItems,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async System.Threading.Tasks.Task LogActionAsync(
        string entityType, 
        Guid entityId, 
        string action, 
        string? oldData = null, 
        string? newData = null, 
        Guid? userId = null, 
        string? ipAddress = null)
    {
        var auditLog = new AuditLog
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldData = oldData,
            NewData = newData,
            UserId = userId,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.AuditLogs.AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Audit log created: {EntityType} {EntityId} - {Action}", entityType, entityId, action);
    }
}
