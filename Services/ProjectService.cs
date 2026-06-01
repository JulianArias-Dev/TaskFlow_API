using TaskFlow_API.DTOs;
using TaskFlow_API.Models;
using TaskFlow_API.Repositories;
using TaskFlow_API.Patterns.Builder;
using TaskFlow_API.Patterns.Prototype;
using TaskFlow_API.Patterns.Adapter;
using TaskFlow_API.Patterns.State;

namespace TaskFlow_API.Services;

/// <summary>
/// Interfaz del servicio de Project
/// Define operaciones de negocio para proyectos
/// </summary>
public interface IProjectService
{
    System.Threading.Tasks.Task<ProjectDto?> GetProjectByIdAsync(Guid id);
    System.Threading.Tasks.Task<IEnumerable<ProjectDto>> GetAllProjectsAsync();
    System.Threading.Tasks.Task<IEnumerable<ProjectDto>> GetProjectsByOwnerAsync(Guid ownerId);
    System.Threading.Tasks.Task<IEnumerable<ProjectDto>> GetProjectsByMemberAsync(Guid userId);
    System.Threading.Tasks.Task<ProjectDto> CreateProjectAsync(CreateProjectDto createProjectDto, Guid ownerId);
    System.Threading.Tasks.Task<ProjectDto> UpdateProjectAsync(Guid id, UpdateProjectDto updateProjectDto);
    System.Threading.Tasks.Task<bool> DeleteProjectAsync(Guid id);
    System.Threading.Tasks.Task<ProjectDto?> CloneProjectAsync(Guid projectId, Guid newOwnerId);
	System.Threading.Tasks.Task<bool> AddMemberAsync(Guid projectId, Guid userId, int projectRoleId);
	System.Threading.Tasks.Task<bool> RemoveMemberAsync(Guid projectId, Guid userId);
    System.Threading.Tasks.Task<bool> IsMemberAsync(Guid projectId, Guid userId);
    System.Threading.Tasks.Task<int> GetProjectCountByOwnerAsync(Guid ownerId);
}

/// <summary>
/// Servicio de Project
/// Encapsula la l�gica de negocio para operaciones con proyectos
/// Utiliza patrones creacionales: Builder, Prototype
/// </summary>
public class ProjectService : IProjectService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

	public ProjectService(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
	}

    public async System.Threading.Tasks.Task<ProjectDto?> GetProjectByIdAsync(Guid id)
    {
        var project = await _unitOfWork.Projects.GetProjectWithTasksAsync(id);
        return project != null ? MapToDto(project) : null;
    }

    public async System.Threading.Tasks.Task<IEnumerable<ProjectDto>> GetAllProjectsAsync()
    {
        var projects = await _unitOfWork.Projects.GetAllProjectsWithTasksAsync();
        return projects.Select(MapToDto).ToList();
    }

    public async System.Threading.Tasks.Task<IEnumerable<ProjectDto>> GetProjectsByOwnerAsync(Guid ownerId)
    {
        var projects = await _unitOfWork.Projects.GetProjectsByOwnerAsync(ownerId);
        return projects.Select(MapToDto).ToList();
    }

    public async System.Threading.Tasks.Task<IEnumerable<ProjectDto>> GetProjectsByMemberAsync(Guid userId)
    {
        var projects = await _unitOfWork.Projects.GetProjectsByMemberAsync(userId);
        return projects.Select(MapToDto).ToList();
    }

    public async System.Threading.Tasks.Task<ProjectDto> CreateProjectAsync(CreateProjectDto createProjectDto, Guid ownerId)
    {
        var builder = new ProjectBuilder(ownerId)
            .WithName(createProjectDto.Name)
            .WithDescription(createProjectDto.Description ?? "")
            .WithColor(createProjectDto.Color)
            .WithStatusId(createProjectDto.StatusId);

        if (createProjectDto.StartDate.HasValue)
            builder = builder.WithStartDate(createProjectDto.StartDate.Value);

        if (createProjectDto.EndDate.HasValue)
            builder = builder.WithEndDate(createProjectDto.EndDate.Value);

        var project = builder.Build();

        await _unitOfWork.Projects.AddAsync(project);

        var defaultBoard = new BoardBuilder()
            .WithBasicInfo("Main Board", $"Default board for {project.Name}", project.Id)
            .WithDefaultColumns()
            .Build();

        await _unitOfWork.Boards.AddAsync(defaultBoard);

        await _unitOfWork.SaveChangesAsync();

        // Recargamos con todas las navegaciones (Status, Boards, Columns,
        // Members) para que MapToDto entregue una respuesta completa.
        var hydrated = await _unitOfWork.Projects.GetProjectWithTasksAsync(project.Id) ?? project;
        return MapToDto(hydrated);
    }

	public async System.Threading.Tasks.Task<ProjectDto> UpdateProjectAsync(Guid id, UpdateProjectDto updateProjectDto)
	{
		var project = await _unitOfWork.Projects.GetByIdAsync(id);
		if (project == null)
		{
			throw new KeyNotFoundException($"Project with ID {id} not found");
		}

		var current = ProjectStateFactory.For(project);

		// Detecta si la request trae campos distintos al estado.
		var wantsFieldChange =
			!string.IsNullOrEmpty(updateProjectDto.Name) ||
			updateProjectDto.Description != null ||
			!string.IsNullOrEmpty(updateProjectDto.Color) ||
			updateProjectDto.StartDate.HasValue ||
			updateProjectDto.EndDate.HasValue;

		// Validación 1: editar campos exige CanEdit() en el estado actual.
		if (wantsFieldChange && !current.CanEdit())
		{
			throw new InvalidOperationException(
				$"No se puede editar el proyecto: el estado '{current.Name}' es de solo lectura.");
		}

		// Validación 2: cambio de StatusId debe ser una transición permitida.
		if (updateProjectDto.StatusId.HasValue
			&& updateProjectDto.StatusId.Value > 0
			&& updateProjectDto.StatusId.Value != project.StatusId)
		{
			if (!current.CanTransitionTo(updateProjectDto.StatusId.Value))
			{
				throw new InvalidOperationException(
					$"Transición no permitida: '{current.Name}' → StatusId {updateProjectDto.StatusId.Value}.");
			}
			project.StatusId = updateProjectDto.StatusId.Value;
		}

		if (!string.IsNullOrEmpty(updateProjectDto.Name))
			project.Name = updateProjectDto.Name;

		if (updateProjectDto.Description != null) // Permite limpiar la descripci�n enviando ""
			project.Description = updateProjectDto.Description;

		if (!string.IsNullOrEmpty(updateProjectDto.Color))
			project.Color = updateProjectDto.Color;

		if (updateProjectDto.StartDate.HasValue)
			project.StartDate = updateProjectDto.StartDate.Value;

		if (updateProjectDto.EndDate.HasValue)
			project.EndDate = updateProjectDto.EndDate.Value;

		project.UpdatedAt = DateTime.UtcNow;

		await _unitOfWork.Projects.UpdateAsync(project);
		await _unitOfWork.SaveChangesAsync();

		return MapToDto(project);
	}

	public async System.Threading.Tasks.Task<bool> DeleteProjectAsync(Guid id)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(id);
        if (project == null)
        {
            return false;
        }

        var state = ProjectStateFactory.For(project);
        if (!state.CanDelete())
        {
            throw new InvalidOperationException(
                $"No se puede eliminar el proyecto: el estado '{state.Name}' no permite eliminación.");
        }

        await _unitOfWork.Projects.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async System.Threading.Tasks.Task<ProjectDto?> CloneProjectAsync(Guid projectId, Guid newOwnerId)
    {
        var originalProject = await _unitOfWork.Projects.GetProjectWithTasksAsync(projectId);
        if (originalProject == null)
        {
            return null;
        }

        var state = ProjectStateFactory.For(originalProject);
        if (!state.CanClone())
        {
            throw new InvalidOperationException(
                $"No se puede clonar el proyecto: el estado '{state.Name}' no permite clonación.");
        }

        var clonedProject = (Project)PrototypeManager.CloneProject(originalProject);
        clonedProject.OwnerId = newOwnerId;
        // El proyecto clonado siempre arranca Activo, independiente del estado del original.
        clonedProject.StatusId = ActivoProjectState.Id;

        await _unitOfWork.Projects.AddAsync(clonedProject);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(clonedProject);
    }

	public async System.Threading.Tasks.Task<bool> AddMemberAsync(Guid projectId, Guid userId, int projectRoleId = 2) // Supongamos que 2 es 'Developer' en tu nueva tabla
	{
		var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
		if (project == null) return false;

		var state = ProjectStateFactory.For(project);
		if (!state.CanAddMembers())
		{
			throw new InvalidOperationException(
				$"No se pueden agregar miembros: el proyecto está en estado '{state.Name}'.");
		}

		var user = await _unitOfWork.Users.GetByIdAsync(userId);
		if (user == null) return false;

		var isMember = await _unitOfWork.Projects.IsMemberAsync(projectId, userId);

		if (!isMember)
		{
			await _unitOfWork.Projects.AddMemberAsync(projectId, userId, projectRoleId);

			await _unitOfWork.SaveChangesAsync();

			string subject = $"Has sido invitado al proyecto: {project.Name}";
			string greeting = $"¡Hola, {user.Name}!";
			string message = $"Has sido agregado al proyecto {project.Name}.";
			string closing = "Ya puedes empezar a colaborar con tu equipo.";
			string content = NotificationContentFormatter.Build(greeting, message, closing);

			await _notificationService.NotifyAsync(userId, "PROJECT_MEMBER_ADDED", subject, content);
		}

		return true;
	}

	public async System.Threading.Tasks.Task<bool> RemoveMemberAsync(Guid projectId, Guid userId)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
        if (project == null)
        {
            return false;
        }

        var state = ProjectStateFactory.For(project);
        if (!state.CanRemoveMembers())
        {
            throw new InvalidOperationException(
                $"No se pueden quitar miembros: el proyecto está en estado '{state.Name}'.");
        }

        await _unitOfWork.Projects.RemoveMemberAsync(projectId, userId);

        return true;
    }

    public async System.Threading.Tasks.Task<bool> IsMemberAsync(Guid projectId, Guid userId)
    {
        return await _unitOfWork.Projects.IsMemberAsync(projectId, userId);
    }

    public async System.Threading.Tasks.Task<int> GetProjectCountByOwnerAsync(Guid ownerId)
    {
        return await _unitOfWork.Projects.GetProjectCountByOwnerAsync(ownerId);
    }

    private ProjectDto MapToDto(Project project)
    {
        var boards = project.Boards ?? new List<Board>();
        var members = project.Members ?? new List<ProjectMember>();

        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Color = project.Color,
            OwnerId = project.OwnerId,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            // Status puede ser null si la query no incluyó la navegación.
            Status = project.Status?.Name ?? string.Empty,
            StatusId = project.StatusId,
            MemberIds = members.Select(m => m.UserId).ToList(),
            MemberCount = members.Count,
            BoardCount = boards.Count,
            Boards = boards.Select(b =>
            {
                var columns = b.Columns ?? new List<Column>();
                return new BoardDto
                {
                    ProjectId = b.ProjectId,
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt,
                    ColumnCount = columns.Count,
                    Columns = columns.Select(c =>
                    {
                        var tasks = c.Tasks ?? new List<TaskFlow_API.Models.Task>();
                        return new ColumnDto
                        {
                            BoardId = c.BoardId,
                            Id = c.Id,
                            Name = c.Name,
                            DisplayOrder = c.DisplayOrder,
                            CreatedAt = c.CreatedAt,
                            UpdatedAt = c.UpdatedAt,
                            TaskCount = tasks.Count,
                            Tasks = tasks.Select(t => new TaskSimpleDto
                            {
                                Id = t.Id,
                                Title = t.Title,
                                Status = t.Status?.Name ?? string.Empty,
                                StatusId = t.StatusId,
                                Priority = t.Priority?.Name ?? string.Empty,
                                PriorityId = t.PriorityId,
                                AssignedUserIds = t.Assignments?.Select(a => a.UserId).ToList() ?? new(),
                                DueDate = t.DueDate
                            }).ToList()
                        };
                    }).ToList()
                };
            }).ToList(),
        };
    }
}
