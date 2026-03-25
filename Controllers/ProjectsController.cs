using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow_API.DTOs;
using TaskFlow_API.Services;

namespace TaskFlow_API.Controllers;

/// <summary>
/// Controller para gestionar operaciones de proyectos (Projects)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IMapper _mapper;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(IProjectService projectService, IMapper mapper, ILogger<ProjectsController> logger)
    {
        _projectService = projectService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene un proyecto por ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ResponseDto<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseDto<ProjectDto>>> GetProjectById(Guid id)
    {
        var project = await _projectService.GetProjectByIdAsync(id);
        if (project == null)
        {
            _logger.LogWarning("Project with ID {ProjectId} not found", id);
            return NotFound(ResponseDto.NotFoundResponse($"Project with ID {id} not found"));
        }
        return Ok(ResponseDto<ProjectDto>.SuccessResponse(project, "Project retrieved successfully"));
    }

    /// <summary>
    /// Obtiene todos los proyectos
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResponseDto<List<ProjectDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDto<List<ProjectDto>>>> GetAllProjects()
    {
        var projects = await _projectService.GetAllProjectsAsync();
        return Ok(ResponseDto<List<ProjectDto>>.SuccessResponse(projects.ToList(), "Projects retrieved successfully"));
    }

    /// <summary>
    /// Obtiene proyectos de un propietario
    /// </summary>
    [HttpGet("owner/{ownerId}")]
    [ProducesResponseType(typeof(ResponseDto<List<ProjectDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDto<List<ProjectDto>>>> GetProjectsByOwner(Guid ownerId)
    {
        var projects = await _projectService.GetProjectsByOwnerAsync(ownerId);
        return Ok(ResponseDto<List<ProjectDto>>.SuccessResponse(projects.ToList(), "Owner projects retrieved successfully"));
    }

    /// <summary>
    /// Obtiene proyectos donde el usuario es miembro
    /// </summary>
    [HttpGet("member/{userId}")]
    [ProducesResponseType(typeof(ResponseDto<List<ProjectDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDto<List<ProjectDto>>>> GetProjectsByMember(Guid userId)
    {
        var projects = await _projectService.GetProjectsByMemberAsync(userId);
        return Ok(ResponseDto<List<ProjectDto>>.SuccessResponse(projects.ToList(), "Member projects retrieved successfully"));
    }

    /// <summary>
    /// Crea un nuevo proyecto (Builder Pattern)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ResponseDto<ProjectDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto<ProjectDto>>> CreateProject(
        CreateProjectDto createProjectDto,
        [FromQuery] Guid ownerId)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
            return BadRequest(ResponseDto.ValidationErrorResponse(errors));
        }

        if (ownerId == Guid.Empty)
        {
            return BadRequest(ResponseDto.ErrorResponse("Owner ID is required", new List<string> { "Owner ID cannot be empty" }));
        }

        try
        {
            var createdProject = await _projectService.CreateProjectAsync(createProjectDto, ownerId);
            _logger.LogInformation("Project '{ProjectName}' created successfully", createdProject.Name);
            return CreatedAtAction(nameof(GetProjectById), new { id = createdProject.Id },
                ResponseDto<ProjectDto>.SuccessResponse(createdProject, "Project created successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(ResponseDto.ErrorResponse(ex.Message, new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Actualiza un proyecto existente
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ResponseDto<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseDto<ProjectDto>>> UpdateProject(Guid id, UpdateProjectDto updateProjectDto)
    {
        try
        {
            var updatedProject = await _projectService.UpdateProjectAsync(id, updateProjectDto);
            _logger.LogInformation("Project {ProjectId} updated successfully", id);
            return Ok(ResponseDto<ProjectDto>.SuccessResponse(updatedProject, "Project updated successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Project with ID {ProjectId} not found for update", id);
            return NotFound(ResponseDto.NotFoundResponse(ex.Message));
        }
    }

    /// <summary>
    /// Elimina un proyecto
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseDto>> DeleteProject(Guid id)
    {
        var result = await _projectService.DeleteProjectAsync(id);
        if (!result)
        {
            _logger.LogWarning("Project with ID {ProjectId} not found for deletion", id);
            return NotFound(ResponseDto.NotFoundResponse($"Project with ID {id} not found"));
        }
        _logger.LogInformation("Project {ProjectId} deleted successfully", id);
        return Ok(ResponseDto.SuccessResponse("Project deleted successfully"));
    }

    /// <summary>
    /// Clona un proyecto (Prototype Pattern)
    /// </summary>
    [HttpPost("{projectId}/clone")]
    [ProducesResponseType(typeof(ResponseDto<ProjectDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseDto<ProjectDto>>> CloneProject(
        Guid projectId,
        [FromQuery] Guid newOwnerId)
    {
        if (newOwnerId == Guid.Empty)
        {
            return BadRequest(ResponseDto.ErrorResponse("New owner ID is required", new List<string> { "New owner ID cannot be empty" }));
        }

        try
        {
            var clonedProject = await _projectService.CloneProjectAsync(projectId, newOwnerId);
            if (clonedProject == null)
            {
                _logger.LogWarning("Project with ID {ProjectId} not found for cloning", projectId);
                return NotFound(ResponseDto.NotFoundResponse($"Project with ID {projectId} not found"));
            }
            _logger.LogInformation("Project {ProjectId} cloned successfully", projectId);
            return CreatedAtAction(nameof(GetProjectById), new { id = clonedProject.Id },
                ResponseDto<ProjectDto>.SuccessResponse(clonedProject, "Project cloned successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(ResponseDto.ErrorResponse(ex.Message, new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Agrega un miembro a un proyecto
    /// </summary>
    [HttpPost("{projectId}/members/{userId}")]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseDto>> AddMember(Guid projectId, Guid userId)
    {
        try
        {
            var result = await _projectService.AddMemberAsync(projectId, userId, 3);
            if (!result)
            {
                _logger.LogWarning("Failed to add member {UserId} to project {ProjectId}", userId, projectId);
                return NotFound(ResponseDto.NotFoundResponse("Project or user not found"));
            }
            _logger.LogInformation("Member {UserId} added to project {ProjectId}", userId, projectId);
            return Ok(ResponseDto.SuccessResponse("Member added successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ResponseDto.ErrorResponse(ex.Message, new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Elimina un miembro de un proyecto
    /// </summary>
    [HttpDelete("{projectId}/members/{userId}")]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseDto>> RemoveMember(Guid projectId, Guid userId)
    {
        try
        {
            var result = await _projectService.RemoveMemberAsync(projectId, userId);
            if (!result)
            {
                _logger.LogWarning("Failed to remove member {UserId} from project {ProjectId}", userId, projectId);
                return NotFound(ResponseDto.NotFoundResponse("Project not found"));
            }
            _logger.LogInformation("Member {UserId} removed from project {ProjectId}", userId, projectId);
            return Ok(ResponseDto.SuccessResponse("Member removed successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ResponseDto.ErrorResponse(ex.Message, new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Verifica si un usuario es miembro de un proyecto
    /// </summary>
    [HttpGet("{projectId}/members/{userId}")]
    [ProducesResponseType(typeof(ResponseDto<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDto<bool>>> IsMember(Guid projectId, Guid userId)
    {
        var isMember = await _projectService.IsMemberAsync(projectId, userId);
        return Ok(ResponseDto<bool>.SuccessResponse(isMember, "Membership verified"));
    }

    /// <summary>
    /// Obtiene el conteo de proyectos por propietario
    /// </summary>
    [HttpGet("count/owner/{ownerId}")]
    [ProducesResponseType(typeof(ResponseDto<int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDto<int>>> GetProjectCountByOwner(Guid ownerId)
    {
        var count = await _projectService.GetProjectCountByOwnerAsync(ownerId);
        return Ok(ResponseDto<int>.SuccessResponse(count, "Project count retrieved successfully"));
    }
}
