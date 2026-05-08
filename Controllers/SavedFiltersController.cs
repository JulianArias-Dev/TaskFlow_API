using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskFlow_API.DTOs;
using TaskFlow_API.Services;

namespace TaskFlow_API.Controllers;

/// <summary>
/// Controller para Filtros Guardados (RF-07.3).
/// Cada usuario sólo puede ver/modificar sus propios filtros.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class SavedFiltersController : ControllerBase
{
    private readonly ISavedFilterService _service;
    private readonly ILogger<SavedFiltersController> _logger;

    public SavedFiltersController(ISavedFilterService service, ILogger<SavedFiltersController> logger)
    {
        _service = service;
        _logger = logger;
    }

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    [HttpGet]
    public async Task<ActionResult<ResponseDto<List<SavedFilterDto>>>> GetMine()
    {
        var items = await _service.GetByUserAsync(CurrentUserId);
        return Ok(ResponseDto<List<SavedFilterDto>>.SuccessResponse(items.ToList(), "Filters retrieved"));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ResponseDto<SavedFilterDto>>> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id, CurrentUserId);
        if (item == null) return NotFound(ResponseDto.NotFoundResponse($"Filter {id} not found"));
        return Ok(ResponseDto<SavedFilterDto>.SuccessResponse(item, "Filter retrieved"));
    }

    [HttpPost]
    public async Task<ActionResult<ResponseDto<SavedFilterDto>>> Create(CreateSavedFilterDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
            return BadRequest(ResponseDto.ValidationErrorResponse(errors));
        }

        var created = await _service.CreateAsync(CurrentUserId, dto);
        _logger.LogInformation("SavedFilter '{Name}' created by user {UserId}", created.Name, CurrentUserId);
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            ResponseDto<SavedFilterDto>.SuccessResponse(created, "Filter created"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ResponseDto<SavedFilterDto>>> Update(Guid id, UpdateSavedFilterDto dto)
    {
        var updated = await _service.UpdateAsync(id, CurrentUserId, dto);
        if (updated == null) return NotFound(ResponseDto.NotFoundResponse($"Filter {id} not found"));
        return Ok(ResponseDto<SavedFilterDto>.SuccessResponse(updated, "Filter updated"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ResponseDto>> Delete(Guid id)
    {
        var ok = await _service.DeleteAsync(id, CurrentUserId);
        if (!ok) return NotFound(ResponseDto.NotFoundResponse($"Filter {id} not found"));
        return Ok(ResponseDto.SuccessResponse("Filter deleted"));
    }
}
