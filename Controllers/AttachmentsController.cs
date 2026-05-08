using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskFlow_API.DTOs;
using TaskFlow_API.Services;

namespace TaskFlow_API.Controllers;

/// <summary>
/// Controller para gestión de archivos adjuntos a tareas (RF-04.7).
/// Maneja upload (multipart/form-data), listado por tarea y borrado.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttachmentsController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AttachmentsController> _logger;

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public AttachmentsController(
        IFileService fileService,
        IWebHostEnvironment env,
        ILogger<AttachmentsController> logger)
    {
        _fileService = fileService;
        _env = env;
        _logger = logger;
    }

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    /// <summary>Sube un archivo y lo asocia a una tarea.</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<ActionResult<ResponseDto<FileDto>>> Upload([FromForm] UploadAttachmentRequest request)
    {
        if (request.File == null || request.File.Length == 0)
            return BadRequest(ResponseDto.ErrorResponse("File is required"));

        if (request.File.Length > MaxFileSizeBytes)
            return BadRequest(ResponseDto.ErrorResponse(
                $"File exceeds {MaxFileSizeBytes / (1024 * 1024)} MB limit"));

        try
        {
            var (storedPath, publicUrl) = await SaveToDiskAsync(request.File, request.TaskId);

            var dto = new UploadFileDto
            {
                File = request.File,
                TaskId = request.TaskId,
                UserId = CurrentUserId
            };

            var created = await _fileService.CreateFileAsync(dto, publicUrl);
            _logger.LogInformation("Attachment {FileName} uploaded for task {TaskId} by user {UserId}",
                request.File.FileName, request.TaskId, CurrentUserId);

            return CreatedAtAction(nameof(GetByTask), new { taskId = request.TaskId },
                ResponseDto<FileDto>.SuccessResponse(created, "Attachment uploaded"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ResponseDto.NotFoundResponse(ex.Message));
        }
    }

    /// <summary>Lista los archivos adjuntos de una tarea.</summary>
    [HttpGet("task/{taskId}")]
    public async Task<ActionResult<ResponseDto<List<FileDto>>>> GetByTask(Guid taskId)
    {
        var files = await _fileService.GetFilesByTaskAsync(taskId);
        return Ok(ResponseDto<List<FileDto>>.SuccessResponse(files.ToList(), "Attachments retrieved"));
    }

    /// <summary>Obtiene metadatos de un archivo.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ResponseDto<FileDto>>> GetById(Guid id)
    {
        var file = await _fileService.GetFileByIdAsync(id);
        if (file == null) return NotFound(ResponseDto.NotFoundResponse($"File {id} not found"));
        return Ok(ResponseDto<FileDto>.SuccessResponse(file, "File retrieved"));
    }

    /// <summary>Descarga el contenido binario del archivo.</summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var file = await _fileService.GetFileByIdAsync(id);
        if (file == null) return NotFound();

        var diskPath = MapUrlToDiskPath(file.FileUrl);
        if (!System.IO.File.Exists(diskPath))
            return NotFound("File missing from storage");

        var bytes = await System.IO.File.ReadAllBytesAsync(diskPath);
        return File(bytes, file.MimeType, file.FileName);
    }

    /// <summary>Elimina un archivo adjunto.</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ResponseDto>> Delete(Guid id)
    {
        var ok = await _fileService.DeleteFileAsync(id);
        if (!ok) return NotFound(ResponseDto.NotFoundResponse($"File {id} not found"));
        return Ok(ResponseDto.SuccessResponse("Attachment deleted"));
    }

    // ----------- helpers -----------

    private async Task<(string storedPath, string publicUrl)> SaveToDiskAsync(IFormFile file, Guid taskId)
    {
        var uploadsRoot = Path.Combine(_env.ContentRootPath, "uploads", taskId.ToString());
        Directory.CreateDirectory(uploadsRoot);

        var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}";
        var diskPath = Path.Combine(uploadsRoot, safeName);

        await using var stream = System.IO.File.Create(diskPath);
        await file.CopyToAsync(stream);

        var publicUrl = $"/uploads/{taskId}/{safeName}";
        return (diskPath, publicUrl);
    }

    private string MapUrlToDiskPath(string fileUrl)
    {
        // fileUrl es "/uploads/{taskId}/{name}" — lo convertimos a path físico.
        var relative = fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_env.ContentRootPath, relative);
    }
}

/// <summary>DTO para la subida (multipart/form-data).</summary>
public class UploadAttachmentRequest
{
    public IFormFile File { get; set; } = null!;
    public Guid TaskId { get; set; }
}
