using TaskFlow_API.DTOs;
using TaskFlow_API.Repositories;
using TaskFlow_API.Services;

namespace TaskFlow_API.Patterns.Proxy;

/// <summary>
/// PATRÓN PROXY — Protection Proxy.
///
/// Implementa la misma interfaz `ITaskService` que el servicio real, y se inyecta
/// en su lugar (ver Program.cs). Cada método de creación/actualización pasa por
/// validaciones de negocio antes de delegar al `TaskService` real:
///
///   - Verifica que el proyecto está activo (no archivado/cancelado).
///   - Comprueba que el usuario actual tiene permisos sobre el proyecto.
///   - Bloquea actualizaciones a tareas en proyectos finalizados.
///
/// Para los métodos de lectura, delega directamente sin overhead.
///
/// Beneficio: el `TaskService` real queda libre de lógica transversal —
/// se concentra en operaciones puras. Si mañana se quieren añadir auditoría,
/// throttling o cache, se hace AGREGANDO PROXIES (Decorator-like) sin tocar
/// el servicio real.
/// </summary>
public class TaskServiceProxy : ITaskService
{
    private readonly ITaskService _real;
    private readonly IUnitOfWork _uow;
    private readonly IHttpContextAccessor _httpContext;
    private readonly ILogger<TaskServiceProxy> _logger;

    private const int ProjectStatusActiveId = 1;
    private const int ProjectStatusCompletedId = 2;
    private const int ProjectStatusCancelledId = 4;

    public TaskServiceProxy(
        ITaskService real,
        IUnitOfWork uow,
        IHttpContextAccessor httpContext,
        ILogger<TaskServiceProxy> logger)
    {
        _real = real;
        _uow = uow;
        _httpContext = httpContext;
        _logger = logger;
    }

    // -------- Operaciones que requieren validación previa --------

    public async System.Threading.Tasks.Task<TaskDto> CreateTaskAsync(CreateTaskDto dto)
    {
        // 1) Validar que la columna existe y obtener proyecto.
        var column = await _uow.Columns.GetByIdAsync(dto.ColumnId)
            ?? throw new KeyNotFoundException("Column not found");
        var board = await _uow.Boards.GetByIdAsync(column.BoardId)
            ?? throw new KeyNotFoundException("Board not found");
        var project = await _uow.Projects.GetByIdAsync(board.ProjectId)
            ?? throw new KeyNotFoundException("Project not found");

        // 2) Proyecto debe estar activo.
        EnsureProjectIsActive(project);

        // 3) Usuario actual debe ser miembro o el dueño.
        await EnsureCurrentUserIsAuthorized(project.Id, project.OwnerId);

        _logger.LogDebug("[Proxy] CreateTask validations passed for project {ProjectId}", project.Id);
        return await _real.CreateTaskAsync(dto);
    }

    public async System.Threading.Tasks.Task<TaskDto> UpdateTaskAsync(Guid id, UpdateTaskDto dto)
    {
        var task = await _uow.Tasks.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Task {id} not found");

        var column = await _uow.Columns.GetByIdAsync(task.ColumnId);
        if (column != null)
        {
            var board = await _uow.Boards.GetByIdAsync(column.BoardId);
            if (board != null)
            {
                var project = await _uow.Projects.GetByIdAsync(board.ProjectId);
                if (project != null)
                {
                    EnsureProjectIsActive(project);
                    await EnsureCurrentUserIsAuthorized(project.Id, project.OwnerId);
                }
            }
        }

        _logger.LogDebug("[Proxy] UpdateTask validations passed for task {TaskId}", id);
        return await _real.UpdateTaskAsync(id, dto);
    }

    public async System.Threading.Tasks.Task<bool> DeleteTaskAsync(Guid id)
    {
        var task = await _uow.Tasks.GetByIdAsync(id);
        if (task != null)
        {
            var column = await _uow.Columns.GetByIdAsync(task.ColumnId);
            var board = column != null ? await _uow.Boards.GetByIdAsync(column.BoardId) : null;
            var project = board != null ? await _uow.Projects.GetByIdAsync(board.ProjectId) : null;

            if (project != null)
            {
                EnsureProjectIsActive(project);
                await EnsureCurrentUserIsAuthorized(project.Id, project.OwnerId);
            }
        }

        return await _real.DeleteTaskAsync(id);
    }

    public async System.Threading.Tasks.Task<TaskDto> CreateTaskByTypeAsync(int taskType, string title, string description, Guid columnId)
    {
        // Necesitamos remontar la cadena Column -> Board -> Project para validar
        // permisos y estado del proyecto antes de delegar al servicio real.
        var column = await _uow.Columns.GetByIdAsync(columnId)
            ?? throw new KeyNotFoundException($"Column {columnId} not found");

        var board = await _uow.Boards.GetByIdAsync(column.BoardId)
            ?? throw new KeyNotFoundException($"Board {column.BoardId} not found");

        var project = await _uow.Projects.GetByIdAsync(board.ProjectId)
            ?? throw new KeyNotFoundException($"Project {board.ProjectId} not found");

        EnsureProjectIsActive(project);
        await EnsureCurrentUserIsAuthorized(board.ProjectId, project.OwnerId);

        return await _real.CreateTaskByTypeAsync(taskType, title, description, columnId);
    }

    public async System.Threading.Tasks.Task<TaskDto?> CloneTaskAsync(Guid taskId)
    {
        var task = await _uow.Tasks.GetByIdAsync(taskId);
        if (task != null)
        {
            var column = await _uow.Columns.GetByIdAsync(task.ColumnId);
            var board = column != null ? await _uow.Boards.GetByIdAsync(column.BoardId) : null;
            var project = board != null ? await _uow.Projects.GetByIdAsync(board.ProjectId) : null;
            if (project != null)
            {
                EnsureProjectIsActive(project);
                await EnsureCurrentUserIsAuthorized(project.Id, project.OwnerId);
            }
        }
        return await _real.CloneTaskAsync(taskId);
    }

    // -------- Operaciones de lectura — delegación pura --------

    public System.Threading.Tasks.Task<TaskDto?> GetTaskByIdAsync(Guid id) => _real.GetTaskByIdAsync(id);
    public System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetAllTasksAsync() => _real.GetAllTasksAsync();
    public System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetTasksByProjectAsync(Guid projectId) => _real.GetTasksByProjectAsync(projectId);
    public System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetTasksByStatusAsync(int status) => _real.GetTasksByStatusAsync(status);
    public System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetTasksByAssigneeAsync(Guid userId) => _real.GetTasksByAssigneeAsync(userId);
    public System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetOverdueTasksAsync() => _real.GetOverdueTasksAsync();
    public System.Threading.Tasks.Task<int> GetTaskCountByProjectAsync(Guid projectId) => _real.GetTaskCountByProjectAsync(projectId);
    public System.Threading.Tasks.Task<int> GetTaskCountByStatusAsync(int status) => _real.GetTaskCountByStatusAsync(status);

    // -------- Helpers de validación --------

    private static void EnsureProjectIsActive(Models.Project project)
    {
        if (project.StatusId == ProjectStatusCompletedId)
            throw new InvalidOperationException("Cannot modify tasks: project is already completed.");
        if (project.StatusId == ProjectStatusCancelledId)
            throw new InvalidOperationException("Cannot modify tasks: project is cancelled.");
    }

    private async System.Threading.Tasks.Task EnsureCurrentUserIsAuthorized(Guid projectId, Guid ownerId)
    {
        var userIdClaim = _httpContext.HttpContext?
            .User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdClaim, out var currentUserId))
        {
            // Sin contexto HTTP (jobs internos, tests) — saltamos check
            return;
        }

        if (currentUserId == ownerId) return;

        var isMember = await _uow.Projects.IsMemberAsync(projectId, currentUserId);
        if (!isMember)
            throw new UnauthorizedAccessException(
                $"User {currentUserId} is not a member of project {projectId}.");
    }
}
