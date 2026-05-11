using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow_API.DTOs;
using TaskFlow_API.Services;

namespace TaskFlow_API.Controllers;

/// <summary>
/// Controller para gestionar operaciones de tareas (Tasks)
/// Proporciona endpoints CRUD y operaciones especializadas
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly IMapper _mapper;
    private readonly ILogger<TasksController> _logger;

    public TasksController(ITaskService taskService, IMapper mapper, ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene una tarea por ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ResponseDto<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseDto<TaskDto>>> GetTaskById(Guid id)
    {
        var task = await _taskService.GetTaskByIdAsync(id);
        if (task == null)
        {
            _logger.LogWarning("Task with ID {TaskId} not found", id);
            return NotFound(ResponseDto.NotFoundResponse($"Task with ID {id} not found"));
        }
        return Ok(ResponseDto<TaskDto>.SuccessResponse(task, "Task retrieved successfully"));
    }

    /// <summary>
    /// Obtiene todas las tareas
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResponseDto<List<TaskDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDto<List<TaskDto>>>> GetAllTasks()
    {
        var tasks = await _taskService.GetAllTasksAsync();
        return Ok(ResponseDto<List<TaskDto>>.SuccessResponse(tasks.ToList(), "Tasks retrieved successfully"));
    }

    /// <summary>
    /// Obtiene tareas de un proyecto espec�fico
    /// </summary>
    [HttpGet("project/{projectId}")]
    [ProducesResponseType(typeof(ResponseDto<List<TaskDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDto<List<TaskDto>>>> GetTasksByProject(Guid projectId)
    {
        var tasks = await _taskService.GetTasksByProjectAsync(projectId);
        return Ok(ResponseDto<List<TaskDto>>.SuccessResponse(tasks.ToList(), "Project tasks retrieved successfully"));
    }

    /// <summary>
    /// Obtiene tareas por estado
    /// </summary>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(ResponseDto<List<TaskDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto<List<TaskDto>>>> GetTasksByStatus(int status)
    {
        try
        {
            var tasks = await _taskService.GetTasksByStatusAsync(status);
            return Ok(ResponseDto<List<TaskDto>>.SuccessResponse(tasks.ToList(), "Tasks retrieved successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid task status: {Status}", status);
            return BadRequest(ResponseDto.ErrorResponse(ex.Message, new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Obtiene tareas asignadas a un usuario
    /// </summary>
    [HttpGet("assignee/{userId}")]
    [ProducesResponseType(typeof(ResponseDto<List<TaskDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDto<List<TaskDto>>>> GetTasksByAssignee(Guid userId)
    {
        var tasks = await _taskService.GetTasksByAssigneeAsync(userId);
        return Ok(ResponseDto<List<TaskDto>>.SuccessResponse(tasks.ToList(), "Assigned tasks retrieved successfully"));
    }

    /// <summary>
    /// Obtiene tareas vencidas
    /// </summary>
    [HttpGet("overdue")]
    [ProducesResponseType(typeof(ResponseDto<List<TaskDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDto<List<TaskDto>>>> GetOverdueTasks()
    {
        var tasks = await _taskService.GetOverdueTasksAsync();
        return Ok(ResponseDto<List<TaskDto>>.SuccessResponse(tasks.ToList(), "Overdue tasks retrieved successfully"));
    }

    /// <summary>
    /// Crea una nueva tarea
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ResponseDto<TaskDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto<TaskDto>>> CreateTask(CreateTaskDto createTaskDto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
            return BadRequest(ResponseDto.ValidationErrorResponse(errors));
        }

        try
        {
            var createdTask = await _taskService.CreateTaskAsync(createTaskDto);
            _logger.LogInformation("Task '{TaskTitle}' created successfully", createdTask.Title);
            return CreatedAtAction(nameof(GetTaskById), new { id = createdTask.Id }, 
                ResponseDto<TaskDto>.SuccessResponse(createdTask, "Task created successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(ResponseDto.ErrorResponse(ex.Message, new List<string> { ex.Message }));
        }
    }

	/// <summary>
	/// Crea una tarea aplicando el patrón Factory Method del PDF — la
	/// configuración por defecto (TypeId, PriorityId) la decide la factoría
	/// correspondiente al taskType. El cliente no necesita conocer los IDs
	/// del catálogo; basta con elegir el tipo.
	/// </summary>
	[HttpPost("by-type")]
	[ProducesResponseType(typeof(ResponseDto<TaskDto>), StatusCodes.Status201Created)]
	[ProducesResponseType(typeof(ResponseDto), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
	public async Task<ActionResult<ResponseDto<TaskDto>>> CreateTaskByType(
		[FromQuery] int taskType,
		[FromQuery] string title,
		[FromQuery] string? description,
		[FromQuery] Guid columnId)
	{
		var errors = new List<string>();
		if (taskType <= 0) errors.Add("Un ID de tipo de tarea válido es requerido");
		if (string.IsNullOrWhiteSpace(title)) errors.Add("El título es requerido");
		if (columnId == Guid.Empty) errors.Add("El ID de la columna es requerido");
		if (errors.Count > 0) return BadRequest(ResponseDto.ValidationErrorResponse(errors));

		try
		{
			var createdTask = await _taskService.CreateTaskByTypeAsync(taskType, title, description ?? "", columnId);
			_logger.LogInformation("Task '{TaskTitle}' created by type ID '{TaskType}'", createdTask.Title, taskType);

			return CreatedAtAction(nameof(GetTaskById), new { id = createdTask.Id },
				ResponseDto<TaskDto>.SuccessResponse(createdTask, "Task created successfully"));
		}
		catch (KeyNotFoundException ex)
		{
			return NotFound(ResponseDto.NotFoundResponse(ex.Message));
		}
		catch (ArgumentException ex)
		{
			return BadRequest(ResponseDto.ErrorResponse(ex.Message, new List<string> { ex.Message }));
		}
	}

	/// <summary>
	/// Actualiza una tarea existente
	/// </summary>
	[HttpPut("{id}")]
    [ProducesResponseType(typeof(ResponseDto<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto<TaskDto>>> UpdateTask(Guid id, UpdateTaskDto updateTaskDto)
    {
        try
        {
            var updatedTask = await _taskService.UpdateTaskAsync(id, updateTaskDto);
            _logger.LogInformation("Task {TaskId} updated successfully", id);
            return Ok(ResponseDto<TaskDto>.SuccessResponse(updatedTask, "Task updated successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Task with ID {TaskId} not found for update", id);
            return NotFound(ResponseDto.NotFoundResponse(ex.Message));
        }
    }

	//// <summary>
	/// Elimina una tarea
	/// </summary>
	[HttpDelete("{id}")]
	[ProducesResponseType(typeof(ResponseDto), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
	[ProducesResponseType(typeof(ResponseDto), StatusCodes.Status400BadRequest)] // Agregamos este tipo de respuesta
	public async Task<ActionResult<ResponseDto>> DeleteTask(Guid id)
	{
		try
		{
			var result = await _taskService.DeleteTaskAsync(id);

			if (!result)
			{
				_logger.LogWarning("Task with ID {TaskId} not found for deletion", id);
				return NotFound(ResponseDto.NotFoundResponse($"Task with ID {id} not found"));
			}

			_logger.LogInformation("Task {TaskId} deleted successfully", id);
			return Ok(ResponseDto.SuccessResponse("Task deleted successfully"));
		}
		catch (InvalidOperationException ex)
		{
			// Capturamos el error de l�gica de negocio (ej. "Tiene subtareas")
			_logger.LogWarning(ex, "Conflict deleting task {TaskId}: {Message}", id, ex.Message);

			// Devolvemos un 400 o 409 con el mensaje que definimos en el Service
			return BadRequest(ResponseDto.FailureResponse(ex.Message));
		}
		catch (Exception ex)
		{
			// Capturamos errores inesperados (DB, Conexi�n, etc.)
			_logger.LogError(ex, "Unexpected error deleting task {TaskId}", id);
			return StatusCode(StatusCodes.Status500InternalServerError,
				ResponseDto.FailureResponse("Ocurri� un error inesperado al eliminar la tarea."));
		}
	}

	/// <summary>
	/// Clona una tarea (Patr�n Prototype)
	/// </summary>
	[HttpPost("{taskId}/clone")]
    [ProducesResponseType(typeof(ResponseDto<TaskDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseDto<TaskDto>>> CloneTask(Guid taskId)
    {
        var clonedTask = await _taskService.CloneTaskAsync(taskId);
        if (clonedTask == null)
        {
            _logger.LogWarning("Task with ID {TaskId} not found for cloning", taskId);
            return NotFound(ResponseDto.NotFoundResponse($"Task with ID {taskId} not found"));
        }
        _logger.LogInformation("Task {TaskId} cloned successfully", taskId);
        return CreatedAtAction(nameof(GetTaskById), new { id = clonedTask.Id },
            ResponseDto<TaskDto>.SuccessResponse(clonedTask, "Task cloned successfully"));
    }

    /// <summary>
    /// Obtiene el conteo de tareas por proyecto
    /// </summary>
    [HttpGet("count/project/{projectId}")]
    [ProducesResponseType(typeof(ResponseDto<int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDto<int>>> GetTaskCountByProject(Guid projectId)
    {
        var count = await _taskService.GetTaskCountByProjectAsync(projectId);
        return Ok(ResponseDto<int>.SuccessResponse(count, "Task count retrieved successfully"));
    }

    /// <summary>
    /// Obtiene el conteo de tareas por estado
    /// </summary>
    [HttpGet("count/status/{status}")]
    [ProducesResponseType(typeof(ResponseDto<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto<int>>> GetTaskCountByStatus(int status)
    {
        try
        {
            var count = await _taskService.GetTaskCountByStatusAsync(status);
            return Ok(ResponseDto<int>.SuccessResponse(count, "Task count retrieved successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ResponseDto.ErrorResponse(ex.Message, new List<string> { ex.Message }));
        }
    }
}
