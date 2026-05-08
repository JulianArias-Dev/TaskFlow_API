using TaskFlow_API.DTOs;
using TaskFlow_API.Patterns.Composite;
using TaskFlow_API.Repositories;
using TaskFlow_API.Services;

namespace TaskFlow_API.Patterns.Facade;

public interface IProjectFacade
{
    System.Threading.Tasks.Task<DashboardDto> GetDashboardAsync(Guid projectId);
}

/// <summary>
/// PATRÓN FACADE.
///
/// Construir el Dashboard requiere ortquestar varios subsistemas:
///   - Repositorio de proyectos (datos del proyecto)
///   - Repositorio de tareas (estados, métricas, vencidas, próximas)
///   - Repositorio de miembros (equipo y workload)
///   - Patrón Composite (cálculo de progreso del árbol de subtareas)
///
/// El controller no debería conocer todos esos servicios. La Facade le ofrece
/// UNA llamada simple — `GetDashboardAsync(projectId)` — que devuelve un único
/// DTO listo para serializar. Cualquier cambio interno (añadir un servicio de
/// auditoría, cambiar el cálculo de velocidad...) queda dentro de la Facade.
/// </summary>
public class ProjectFacade : IProjectFacade
{
    private readonly IUnitOfWork _uow;
    private readonly ITaskService _taskService;
    private readonly IUserService _userService;
    private readonly ILogger<ProjectFacade> _logger;

    private const int InProgressStatusId = 2;
    private const int DoneStatusId = 3;
    private const int BlockedStatusId = 4;

    public ProjectFacade(
        IUnitOfWork uow,
        ITaskService taskService,
        IUserService userService,
        ILogger<ProjectFacade> logger)
    {
        _uow = uow;
        _taskService = taskService;
        _userService = userService;
        _logger = logger;
    }

    public async System.Threading.Tasks.Task<DashboardDto> GetDashboardAsync(Guid projectId)
    {
        var project = await _uow.Projects.GetProjectWithTasksAsync(projectId)
            ?? throw new KeyNotFoundException($"Project {projectId} not found");

        // Subsistema 1 — tareas del proyecto
        var tasks = (await _uow.Tasks.GetTasksByProjectAsync(projectId)).ToList();

        // Subsistema 2 — métricas vía Composite (progreso global como árbol)
        var rootTasks = tasks.Where(t => t.ParentTaskId == null).ToList();
        var compositeProgress = CalculateGlobalProgress(rootTasks);

        // Subsistema 3 — workload por miembro
        var workloads = project.Members
            .Select(m =>
            {
                var assigned = tasks.Where(t => t.Assignments.Any(a => a.UserId == m.UserId)).ToList();
                return new MemberWorkloadDto
                {
                    UserId = m.UserId,
                    UserName = m.User?.Name ?? "Unknown",
                    AssignedTasks = assigned.Count,
                    CompletedTasks = assigned.Count(t => t.StatusId == DoneStatusId)
                };
            }).ToList();

        // Subsistema 4 — próximas fechas límite (top 5)
        var upcoming = tasks
            .Where(t => t.DueDate.HasValue && t.DueDate >= DateTime.UtcNow && t.StatusId != DoneStatusId)
            .OrderBy(t => t.DueDate)
            .Take(5)
            .Select(t => new UpcomingTaskDto
            {
                TaskId = t.Id,
                Title = t.Title,
                DueDate = t.DueDate,
                PriorityId = t.PriorityId
            }).ToList();

        var dashboard = new DashboardDto
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            ProjectStatus = project.Status?.Name ?? "Unknown",
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            TotalTasks = tasks.Count,
            CompletedTasks = tasks.Count(t => t.StatusId == DoneStatusId),
            InProgressTasks = tasks.Count(t => t.StatusId == InProgressStatusId),
            BlockedTasks = tasks.Count(t => t.StatusId == BlockedStatusId),
            OverdueTasks = tasks.Count(t => t.DueDate < DateTime.UtcNow && t.StatusId != DoneStatusId),
            CompletionPercentage = compositeProgress,
            MemberCount = project.Members.Count,
            MemberWorkloads = workloads,
            TotalEstimatedHours = tasks.Sum(t => t.EstimatedHours),
            TotalActualHours = tasks.Sum(t => t.ActualHours),
            UpcomingDeadlines = upcoming,
            GeneratedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Dashboard generated for project {ProjectId} ({Total} tasks)", projectId, tasks.Count);
        return dashboard;
    }

    /// <summary>
    /// Usa el patrón Composite para calcular un progreso global ponderado:
    /// cada tarea raíz contribuye según su peso, y dentro suya las subtareas
    /// se promedian recursivamente.
    /// </summary>
    private static double CalculateGlobalProgress(List<Models.Task> roots)
    {
        if (roots.Count == 0) return 0.0;
        var components = roots.Select(TaskCompositeBuilder.Build).ToList();
        var totalWeight = components.Sum(c => c.Weight);
        if (totalWeight == 0) return 0.0;
        var weightedSum = components.Sum(c => c.GetProgress() * c.Weight);
        return Math.Round(weightedSum / totalWeight, 2);
    }
}
