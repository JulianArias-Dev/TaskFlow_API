using AutoMapper;
using TaskFlow_API.DTOs;
using TaskFlow_API.Models;
using TaskFlow_API.Repositories;
using FileEntity = TaskFlow_API.Models.File;

namespace TaskFlow_API.Services;

/// <summary>
/// Interfaz del servicio de File
/// </summary>
public interface IFileService
{
    System.Threading.Tasks.Task<FileDto?> GetFileByIdAsync(Guid id);
    System.Threading.Tasks.Task<IEnumerable<FileDto>> GetFilesByTaskAsync(Guid taskId);
    System.Threading.Tasks.Task<(List<FileDto> items, int totalCount)> GetFilesPagedByTaskAsync(Guid taskId, int pageNumber, int pageSize);
    System.Threading.Tasks.Task<IEnumerable<FileDto>> GetFilesByUserAsync(Guid userId);
    System.Threading.Tasks.Task<FileDto> CreateFileAsync(UploadFileDto uploadFileDto, string fileUrl);
    System.Threading.Tasks.Task<bool> DeleteFileAsync(Guid id);
    System.Threading.Tasks.Task<long> GetTotalFileSizeByTaskAsync(Guid taskId);
}

/// <summary>
/// Servicio de File
/// </summary>
public class FileService : IFileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<FileService> _logger;

    public FileService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<FileService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async System.Threading.Tasks.Task<FileDto?> GetFileByIdAsync(Guid id)
    {
        var file = await _unitOfWork.Files.GetByIdAsync(id);
        return file != null ? _mapper.Map<FileDto>(file) : null;
    }

    public async System.Threading.Tasks.Task<IEnumerable<FileDto>> GetFilesByTaskAsync(Guid taskId)
    {
        var files = await _unitOfWork.Files.GetFilesByTaskAsync(taskId);
        return _mapper.Map<IEnumerable<FileDto>>(files);
    }

    public async System.Threading.Tasks.Task<(List<FileDto> items, int totalCount)> GetFilesPagedByTaskAsync(Guid taskId, int pageNumber, int pageSize)
    {
        var (items, totalCount) = await _unitOfWork.Files.GetFilesPagedByTaskAsync(taskId, pageNumber, pageSize);
        var mappedItems = _mapper.Map<List<FileDto>>(items);
        return (mappedItems, totalCount);
    }

    public async System.Threading.Tasks.Task<IEnumerable<FileDto>> GetFilesByUserAsync(Guid userId)
    {
        var files = await _unitOfWork.Files.GetFilesByUserAsync(userId);
        return _mapper.Map<IEnumerable<FileDto>>(files);
    }

    public async System.Threading.Tasks.Task<FileDto> CreateFileAsync(UploadFileDto uploadFileDto, string fileUrl)
    {
        // Validar que la tarea existe
        if (!await _unitOfWork.Tasks.ExistsAsync(uploadFileDto.TaskId))
        {
            throw new KeyNotFoundException($"Task with ID {uploadFileDto.TaskId} not found");
        }

        // Validar que el usuario existe
        if (!await _unitOfWork.Users.ExistsAsync(uploadFileDto.UserId))
        {
            throw new KeyNotFoundException($"User with ID {uploadFileDto.UserId} not found");
        }

        // Validar tamaño de archivo
        const long maxFileSize = 10 * 1024 * 1024; // 10 MB
        if (uploadFileDto.File.Length > maxFileSize)
        {
            throw new InvalidOperationException("File size exceeds maximum allowed size of 10 MB");
        }

        var file = new FileEntity
        {
            FileName = uploadFileDto.File.FileName,
            FileUrl = fileUrl,
            MimeType = uploadFileDto.File.ContentType,
            FileSize = uploadFileDto.File.Length,
            TaskId = uploadFileDto.TaskId,
            UploadedByUserId = uploadFileDto.UserId
        };

        await _unitOfWork.Files.AddAsync(file);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("File '{FileName}' uploaded for task {TaskId}", file.FileName, file.TaskId);

        // Recargar para obtener relaciones
        var createdFile = await _unitOfWork.Files.GetByIdAsync(file.Id);
        return _mapper.Map<FileDto>(createdFile)!;
    }

    public async System.Threading.Tasks.Task<bool> DeleteFileAsync(Guid id)
    {
        var file = await _unitOfWork.Files.GetByIdAsync(id);
        if (file == null)
        {
            return false;
        }

        await _unitOfWork.Files.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("File {FileId} deleted", id);
        return true;
    }

    public async System.Threading.Tasks.Task<long> GetTotalFileSizeByTaskAsync(Guid taskId)
    {
        return await _unitOfWork.Files.GetTotalSizeByTaskAsync(taskId);
    }
}
