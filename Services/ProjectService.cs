using TaskFlow_API.DTOs;
using TaskFlow_API.Models;
using TaskFlow_API.Repositories;
using TaskFlow_API.Patterns.Builder;
using TaskFlow_API.Patterns.Prototype;

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
/// Encapsula la lógica de negocio para operaciones con proyectos
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
        var project = new ProjectBuilder(ownerId)
            .WithName(createProjectDto.Name)
            .WithDescription(createProjectDto.Description ?? "")
            .WithColor(createProjectDto.Color)
            .WithStartDate(Convert.ToDateTime(createProjectDto.StartDate))
            .Build();

        await _unitOfWork.Projects.AddAsync(project);

        var defaultBoard = new BoardBuilder()
            .WithBasicInfo("Main Board", $"Default board for {project.Name}", project.Id)
            .WithDefaultColumns()
            .Build();

		await _unitOfWork.Boards.AddAsync(defaultBoard);

		await _unitOfWork.SaveChangesAsync();
        
		return MapToDto(project);
    }

	public async System.Threading.Tasks.Task<ProjectDto> UpdateProjectAsync(Guid id, UpdateProjectDto updateProjectDto)
	{
		var project = await _unitOfWork.Projects.GetByIdAsync(id);
		if (project == null)
		{
			throw new KeyNotFoundException($"Project with ID {id} not found");
		}

		if (!string.IsNullOrEmpty(updateProjectDto.Name))
			project.Name = updateProjectDto.Name;

		if (updateProjectDto.Description != null) // Permite limpiar la descripción enviando ""
			project.Description = updateProjectDto.Description;

		if (!string.IsNullOrEmpty(updateProjectDto.Color))
			project.Color = updateProjectDto.Color;

		if (updateProjectDto.StartDate.HasValue)
			project.StartDate = updateProjectDto.StartDate.Value;

		if (updateProjectDto.EndDate.HasValue)
			project.EndDate = updateProjectDto.EndDate.Value;

		if (updateProjectDto.StatusId.HasValue && updateProjectDto.StatusId.Value > 0)
		{
			project.StatusId = updateProjectDto.StatusId.Value;
		}

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

        var clonedProject = (Project)PrototypeManager.CloneProject(originalProject);
        clonedProject.OwnerId = newOwnerId;

        await _unitOfWork.Projects.AddAsync(clonedProject);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(clonedProject);
    }

	public async System.Threading.Tasks.Task<bool> AddMemberAsync(Guid projectId, Guid userId, int projectRoleId = 2) // Supongamos que 2 es 'Developer' en tu nueva tabla
	{
		var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
		if (project == null) return false;

		var user = await _unitOfWork.Users.GetByIdAsync(userId);
		if (user == null) return false;

		var isMember = await _unitOfWork.Projects.IsMemberAsync(projectId, userId);

		if (!isMember)
		{
			await _unitOfWork.Projects.AddMemberAsync(projectId, userId, projectRoleId);

			await _unitOfWork.SaveChangesAsync();

			string subject = $"Has sido invitado al proyecto: {project.Name}";
			string content = $@"
            <h3>¡Hola, {user.Name}!</h3>
            <p>Has sido agregado al proyecto <strong>{project.Name}</strong>.</p>
            <p>Ya puedes empezar a colaborar con el equipo.</p>";

			await _notificationService.NotifyAsync(userId, subject, content);
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
            Status = project.Status.ToString(),
			MemberIds = project.Members?.Select(m => m.UserId).ToList() ?? new(),
            //Tasks = project.Tasks?.Select(t => new TaskDto
            //{
            //    Id = t.Id,
            //    Title = t.Title,
            //    Description = t.Description,
            //    Type = t.Type.ToString(),
            //    Status = t.Status.ToString(),
            //    Priority = t.Priority.ToString(),
            //    ProjectId = t.ProjectId,
            //    AssignedToUserId = t.AssignedToUserId,
            //    CreatedAt = t.CreatedAt,
            //    UpdatedAt = t.UpdatedAt,
            //    DueDate = t.DueDate,
            //    EstimatedHours = t.EstimatedHours,
            //    Tags = t.Tags
            //}).ToList() ?? new(),
            BoardCount = project.Boards.Count,
            Boards = project.Boards?.Select(b => new BoardDto
            {
                ProjectId = b.ProjectId,
				Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
                ColumnCount = b.Columns.Count,
                Columns = b.Columns?.Select(c => new ColumnDto
                {
                    BoardId = c.BoardId,
					Id = c.Id,
                    Name = c.Name,
                    DisplayOrder = c.DisplayOrder,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    TaskCount = c.Tasks.Count,
                    Tasks = c.Tasks?.Select(t => new TaskSimpleDto
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Status = t.Status.ToString(),
                        Priority = t.Priority.ToString(),
						AssignedUserIds = t.Assignments?.Select(a => a.UserId).ToList() ?? new(),
						DueDate = t.DueDate
					}
                    ).ToList () ?? new()
				}).ToList() ?? new()
            }).ToList() ?? new(),
		};
    }
}
